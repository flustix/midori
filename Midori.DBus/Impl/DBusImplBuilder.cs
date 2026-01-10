using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;

namespace Midori.DBus.Impl;

[RequiresDynamicCode("")]
internal class DBusImplBuilder<T>
    where T : class
{
    private const string asm_mod = "Midori.DBus.TypeImpls";

    public static T Build(DBusConnection connection)
    {
        var name = new AssemblyName(asm_mod);
        var builder = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);
        var module = builder.DefineDynamicModule(asm_mod);
        var type = createImpl(module);

        var factory = type.GetMethod(nameof(Build), BindingFlags.Public | BindingFlags.Static);
        var factoryDelegate = (Func<DBusConnection, T>)factory!.CreateDelegate(typeof(Func<DBusConnection, T>));
        return factoryDelegate(connection);
    }

    private static Type createImpl(ModuleBuilder module)
    {
        var name = $"{asm_mod}.{typeof(T).Name}Impl";
        var type = module.DefineType(name, TypeAttributes.Public, typeof(DBusObject), [typeof(T)]);

        var connection = type.DefineField("connection", typeof(DBusConnection), FieldAttributes.Private | FieldAttributes.InitOnly);
        var ctor = buildCtor(type, connection);
        buildFactory(type, ctor);

        foreach (var method in queryMethods(typeof(T)))
            buildMethod(type, method, connection);

        return type.CreateType();
    }

    private static void buildMethod(TypeBuilder type, MethodInfo inMethod, FieldInfo connection)
    {
        var parameters = inMethod.GetParameters();
        var parameterTypes = parameters.Select(p => p.ParameterType).ToArray();

        var returnType = inMethod.ReturnType;
        var subReturnType = returnType.GetGenericArguments().First();

        var method = type.DefineMethod(inMethod.Name, MethodAttributes.Public
                                                      | MethodAttributes.Virtual
                                                      | MethodAttributes.Final
                                                      | MethodAttributes.HideBySig
                                                      | MethodAttributes.NewSlot);

        method.SetReturnType(returnType);
        method.SetParameters(parameterTypes);

        var gen = method.GetILGenerator();
        gen.DeclareLocal(typeof(List<object>));
        gen.DeclareLocal(typeof(DBusMessage));

        gen.Emit(OpCodes.Newobj, typeof(List<object>).GetConstructor(BindingFlags.Public | BindingFlags.Instance, [])!);
        gen.Emit(OpCodes.Stloc_0); // list =

        var listAdd = typeof(List<object>).GetMethod(nameof(List<object>.Add), BindingFlags.Instance | BindingFlags.Public);

        for (var i = 0; i < parameters.Length; i++)
        {
            var p = parameters[i].ParameterType;

            gen.Emit(OpCodes.Ldloc_0);
            gen.Emit(OpCodes.Ldarg, i + 1);

            if (p.IsValueType)
                gen.Emit(OpCodes.Box, p);

            gen.Emit(OpCodes.Callvirt, listAdd!); // list.Add
            gen.Emit(OpCodes.Nop);
        }

        gen.Emit(OpCodes.Ldarg_0); // this
        gen.Emit(OpCodes.Ldfld, connection); // connection
        gen.Emit(OpCodes.Ldarg_0); // this
        gen.Emit(OpCodes.Ldstr, method.Name); // member
        gen.Emit(OpCodes.Ldloc_0); // list

        var inv = typeof(DBusConnection).GetMethod(nameof(DBusConnection.CallFromProxy), BindingFlags.NonPublic | BindingFlags.Instance);
        gen.Emit(OpCodes.Callvirt, inv!); // connection.CallFromProxy()

        var getResult = typeof(Task<DBusMessage>).GetMethod("get_" + nameof(Task<DBusMessage>.Result), BindingFlags.Public | BindingFlags.Instance)!;
        gen.Emit(OpCodes.Callvirt, getResult); // task.Result
        gen.Emit(OpCodes.Stloc_1); // result =

        gen.Emit(OpCodes.Ldc_I4_0);
        var fromResult = typeof(Task).GetMethod(nameof(Task.FromResult), BindingFlags.Public | BindingFlags.Static)!
                                     .MakeGenericMethod(subReturnType);
        gen.Emit(OpCodes.Call, fromResult);
        gen.Emit(OpCodes.Ret);
    }

    private static ConstructorBuilder buildCtor(TypeBuilder type, FieldInfo connection)
    {
        var ctor = type.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, [typeof(DBusConnection)]);
        var gen = ctor.GetILGenerator();

        gen.Emit(OpCodes.Ldarg_0); // this
        gen.Emit(OpCodes.Call, typeof(DBusObject).GetConstructors().Single()); // : base()

        gen.Emit(OpCodes.Ldarg_0); // this
        gen.Emit(OpCodes.Ldarg_1); // connection
        gen.Emit(OpCodes.Stfld, connection); // this.connection = connection
        gen.Emit(OpCodes.Ret);

        return ctor;
    }

    private static void buildFactory(TypeBuilder type, ConstructorInfo ctor)
    {
        var method = type.DefineMethod(nameof(Build), MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, typeof(T), [typeof(DBusConnection)]);
        var gen = method.GetILGenerator();

        gen.Emit(OpCodes.Ldarg_0); // connection
        gen.Emit(OpCodes.Newobj, ctor); // t = new()
        gen.Emit(OpCodes.Ret); // return t
    }

    private static IEnumerable<MethodInfo> queryMethods(Type interfaceType)
    {
        foreach (var method in interfaceType.GetMethods())
            yield return method;
    }
}

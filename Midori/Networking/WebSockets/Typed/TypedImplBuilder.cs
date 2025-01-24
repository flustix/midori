using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using Midori.Networking.WebSockets.Typed.Proxy;

namespace Midori.Networking.WebSockets.Typed;

// HEAVILY taken from SignalR hubs
[RequiresDynamicCode("")]
internal class TypedImplBuilder<T>
    where T : class
{
    private const string asm_mod = "Midori.TypeImpls";

    private static readonly PropertyInfo cancelNone = typeof(CancellationToken).GetProperty("None", BindingFlags.Public | BindingFlags.Static)!;
    private static readonly ConstructorInfo baseCtor = typeof(object).GetConstructors().Single();
    private static readonly Type[] ctorParameters = { typeof(ITypedProxy) };

    public static T Build(ITypedProxy proxy)
    {
        var name = new AssemblyName(asm_mod);
        var builder = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);
        var module = builder.DefineDynamicModule(asm_mod);
        var type = createImpl(module);

        var factory = type.GetMethod(nameof(Build), BindingFlags.Public | BindingFlags.Static);
        var factoryDelegate = (Func<ITypedProxy, T>)factory!.CreateDelegate(typeof(Func<ITypedProxy, T>));
        return factoryDelegate(proxy);
    }

    private static Type createImpl(ModuleBuilder module)
    {
        var name = $"{asm_mod}.{typeof(T).Name}Impl";
        var type = module.DefineType(name, TypeAttributes.Public, typeof(object), new[] { typeof(T) });

        var proxy = type.DefineField("proxy", typeof(ITypedProxy), FieldAttributes.Private | FieldAttributes.InitOnly);
        var ctor = buildCtor(type, proxy);

        buildFactory(type, ctor);

        foreach (var method in queryMethods(typeof(T)))
            buildMethod(type, method, proxy);

        return type.CreateType();
    }

    private static ConstructorBuilder buildCtor(TypeBuilder type, FieldInfo proxy)
    {
        var ctor = type.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, ctorParameters);
        var gen = ctor.GetILGenerator();

        gen.Emit(OpCodes.Ldarg_0);
        gen.Emit(OpCodes.Call, baseCtor);

        gen.Emit(OpCodes.Ldarg_0);
        gen.Emit(OpCodes.Ldarg_1);
        gen.Emit(OpCodes.Stfld, proxy);
        gen.Emit(OpCodes.Ret);

        return ctor;
    }

    private static void buildFactory(TypeBuilder type, ConstructorInfo ctor)
    {
        var method = type.DefineMethod(nameof(Build), MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, typeof(T), ctorParameters);
        var gen = method.GetILGenerator();

        gen.Emit(OpCodes.Ldarg_0);
        gen.Emit(OpCodes.Newobj, ctor);
        gen.Emit(OpCodes.Ret);
    }

    private static void buildMethod(TypeBuilder type, MethodInfo inMethod, FieldInfo proxy)
    {
        var parameters = inMethod.GetParameters();
        var parameterTypes = parameters.Select(p => p.ParameterType).ToArray();
        var returnType = inMethod.ReturnType;
        var isInvoke = returnType != typeof(Task);

        var method = type.DefineMethod(inMethod.Name, MethodAttributes.Public
                                                      | MethodAttributes.Virtual
                                                      | MethodAttributes.Final
                                                      | MethodAttributes.HideBySig
                                                      | MethodAttributes.NewSlot);

        MethodInfo invoke;

        if (isInvoke)
        {
            invoke = typeof(ITypedSingleProxy).GetMethod(nameof(ITypedSingleProxy.PerformWithReturnAsync), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                new[] { typeof(string), typeof(object[]), typeof(CancellationToken) })!.MakeGenericMethod(returnType.GenericTypeArguments);
        }
        else
        {
            invoke = typeof(ITypedProxy).GetMethod(nameof(ITypedProxy.PerformAsync), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                new[] { typeof(string), typeof(object[]), typeof(CancellationToken) })!;
        }

        method.SetReturnType(returnType);
        method.SetParameters(parameterTypes);

        var generics = parameterTypes.Where(p => p.IsGenericParameter)
                                     .Select(p => p.Name).Distinct().ToArray();

        if (generics.Length > 0)
            method.DefineGenericParameters(generics);

        var hasCancel = parameterTypes.LastOrDefault() == typeof(CancellationToken);

        if (hasCancel)
            parameterTypes = parameterTypes.Take(parameterTypes.Length - 1).ToArray();

        var gen = method.GetILGenerator();

        gen.DeclareLocal(typeof(object[]));
        gen.Emit(OpCodes.Ldarg_0);
        gen.Emit(OpCodes.Ldfld, proxy);

        if (isInvoke)
        {
            var label = gen.DefineLabel();
            var singleProxy = typeof(ITypedSingleProxy);

            gen.Emit(OpCodes.Isinst, singleProxy);
            gen.Emit(OpCodes.Brtrue_S, label);

            gen.Emit(OpCodes.Ldstr, "Invoke with return values only works with singular clients.");
            gen.Emit(OpCodes.Newobj, typeof(InvalidOperationException).GetConstructor(new[] { typeof(string) })!);
            gen.Emit(OpCodes.Throw);

            gen.MarkLabel(label);
            gen.Emit(OpCodes.Ldarg_0);
            gen.Emit(OpCodes.Ldfld, proxy);
            gen.Emit(OpCodes.Castclass, singleProxy);
        }

        gen.Emit(OpCodes.Ldstr, inMethod.Name);

        gen.Emit(OpCodes.Ldc_I4, parameterTypes.Length);
        gen.Emit(OpCodes.Newarr, typeof(object));
        gen.Emit(OpCodes.Stloc_0);

        for (var i = 0; i < parameterTypes.Length; i++)
        {
            gen.Emit(OpCodes.Ldloc_0);
            gen.Emit(OpCodes.Ldc_I4, i);
            gen.Emit(OpCodes.Ldarg, i + 1);
            gen.Emit(OpCodes.Box, parameterTypes[i]);
            gen.Emit(OpCodes.Stelem_Ref);
        }

        gen.Emit(OpCodes.Ldloc_0);

        if (hasCancel)
            gen.Emit(OpCodes.Ldarg, parameterTypes.Length + 1);
        else
            gen.Emit(OpCodes.Call, cancelNone.GetMethod!);

        gen.Emit(OpCodes.Callvirt, invoke);
        gen.Emit(OpCodes.Ret);
    }

    private static IEnumerable<MethodInfo> queryMethods(Type interfaceType)
    {
        foreach (var method in interfaceType.GetMethods())
            yield return method;
    }
}

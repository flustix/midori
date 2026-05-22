using System;
using System.Collections.Generic;
using Midori.API.Components;
using Midori.Logging;
using Midori.Utils;
using NUnit.Framework;

namespace Midori.Tests.API;

public class TestParameterParsing
{
    [Test]
    [TestCase("username=meow&password=mrrp", ExpectedResult = """{"username":"meow","password":"mrrp"}""")]
    [TestCase("username=meow+meow&password=mrrp", ExpectedResult = """{"username":"meow meow","password":"mrrp"}""")]
    [TestCase("username=meow&password=mrrp%3D%26%21", ExpectedResult = """{"username":"meow","password":"mrrp=&!"}""")]
    [TestCase("value=%2B+%2B+%2B", ExpectedResult = """{"value":"+ + +"}""")]
    public string TestFormUrlEncodedParsing(string input)
    {
        var result = FormUrlEncodedRequestBodyContent.Parse(input);
        var text = result.Serialize();
        Logger.Log(text);
        return text;
    }

    [Test]
    [TestCase("12", typeof(int), ExpectedResult = 12)]
    [TestCase("24.22", typeof(double), ExpectedResult = 24.22d)]
    public object? TestValueParsing(string input, Type type)
    {
        var form = new FormUrlEncodedRequestBodyContent(new Dictionary<string, string>
        {
            { "value", input }
        });

        return form.GetFormEntry(type, "value");
    }
}

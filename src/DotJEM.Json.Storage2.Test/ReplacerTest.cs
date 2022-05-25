using DotJEM.Json.Storage2;
using NUnit.Framework;

namespace Systematic.Odiss.Server.Data.Test;

[TestFixture]
public class ReplacerTest
{
    [TestCase("foo")]
    [TestCase("value with @ in it")]
    [TestCase("value with @ and { and } in it")]
    public void Replace_NoReplacements_ReturnsOriginalValue(string value)
    {
        Assert.That(Replacer.Replace(value, new Dictionary<string, string>()), Is.EqualTo(value));
    }

    [TestCase("foo @{value} me", "value:bar", "foo bar me")]
    [TestCase("foo @{value} me and also @{value} me as well", "value:bar", "foo bar me and also bar me as well")]
    [TestCase("foo @{value_a} me and also @{value_b} me as well", "value_a:bar,value_b:buz", "foo bar me and also buz me as well")]
    public void Replace_Replacements_ReturnsReplacedValue(string input, string replacements, string expected)
    {
        Dictionary<string, string> map = replacements.Split(',')
            .ToDictionary(s => s.Split(':').First(), s => s.Split(':').Last());

        Assert.That(Replacer.Replace(input, map), Is.EqualTo(expected));
    }
}
using System;
using Jellyfin.Plugin.MyAnimeList.Sync.Helpers;
using NUnit.Framework;
using Assert = NUnit.Framework.Legacy.ClassicAssert;

namespace Jellyfin.Plugin.MyAnimeList.Tests.Sync.HelperTests; 

public class StringFormatterTests {

    [Test]
    [TestCase("t3&s7!-", "t3s7")]
    [TestCase("!\"$%^&*", "")]
    public void RemoveSpecialCharactersFromString(string input, string expected) =>
        Assert.AreEqual(StringFormatter.RemoveSpecialCharacters(input), expected);

    [Test]
    [TestCase("Lorem ipsum dolor sit amet, consectetur adipiscing elit", "Loremipsumdolorsitamet,consecteturadipiscingelit")]
    [TestCase("! \"12 3abc%& $^", "!\"123abc%&$^")]
    public void RemoveSpacesFromString(string input, string expected) =>
        Assert.AreEqual(StringFormatter.RemoveSpaces(input), expected);
}
using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace Party.Shared.Tests
{
    public class VamSavesTests
    {
        [Test]
        public void ListsAllScenes()
        {
            var testsSavesDirectory = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "..", "..", "..", "..", "TestData", "Saves"));
            var saves = new VamSaves(testsSavesDirectory);

            var scenes = saves.GetAllScenes();

            Assert.That(scenes.Select(scenes => scenes.Filename), Is.EquivalentTo(new[] { "My Scene 1.json" }));
        }
    }
}

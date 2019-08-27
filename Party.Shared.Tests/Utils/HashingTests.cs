using NUnit.Framework;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;

namespace Party.Shared.Utils
{
	public class HashingTests
	{
		[Test]
		public void CanHashLines()
		{
			var result = Hashing.GetHash(new[]
					{
					"Line 1",
					"Line 2"
					});

			Assert.That(result, Is.EqualTo("9140DDC651FB3861322111773BEE1AFD59DB94A6DCBA56212A5CABD8AAAF6874"));
		}

		[Test]
		public void IgnoreEmptyLines()
		{
			var result = Hashing.GetHash(new[]
					{
					"Line 1",
					"",
					"Line 2",
					""
					});

			Assert.That(result, Is.EqualTo("9140DDC651FB3861322111773BEE1AFD59DB94A6DCBA56212A5CABD8AAAF6874"));
		}

		[Test]
		public async Task CanHashFile()
		{
			var fs = new MockFileSystem(new Dictionary<string, MockFileData>
				{
					{ @"C:\script.cs", new MockFileData("File content..") }
				});

			var result = await Hashing.GetHashAsync(fs, @"C:\script.cs");

			Assert.That(result, Is.EqualTo("BFB3624FAA64B4D563217F63BD1047E88555AAFC3544597253FE4E32E21E7DCB"));
		}

		[Test]
		public async Task FileHashesIgnoreLineBreakTypes()
		{
			var fs = new MockFileSystem(new Dictionary<string, MockFileData>
				{
					{ @"C:\script1.cs", new MockFileData("a\nb\r\nc\n") },
					{ @"C:\script2.cs", new MockFileData("a\nb\nc") }
				});

			var result1 = await Hashing.GetHashAsync(fs, @"C:\script1.cs");
			var result2 = await Hashing.GetHashAsync(fs, @"C:\script2.cs");

			Assert.That(result1, Is.EqualTo(result2));
		}
	}
}

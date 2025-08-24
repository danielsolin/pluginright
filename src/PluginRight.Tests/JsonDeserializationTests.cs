using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using NUnit.Framework;
using PluginRight.Core.Models;
using PluginRight.Core.Utilities;

namespace PluginRight.Tests
{
    /// <summary>
    /// Unit tests for JSON deserialization.
    /// </summary>
    [TestFixture]
    public class JsonDeserializationTests
    {
        [Test]
        public async Task DeserializeJob_FromMinimalJson_Success()
        {
            // Arrange
            var repoRoot = PathUtilities.GetRepositoryRoot();
            var jobFilePath = Path.Combine(repoRoot, "orders/test-mini.json");
            var jobJson = await File.ReadAllTextAsync(jobFilePath);

            // Act
            var job = JsonSerializer.Deserialize<Job>(
                jobJson,
                JsonOptions.Default
            );

            // Assert
            Assert.That(job, Is.Not.Null, "Job deserialization returned null.");
            Assert.That(job!.Entity, Is.EqualTo("account"));
            Assert.That(job.Message, Is.EqualTo("Create"));
            Assert.That(job.Stage, Is.EqualTo(40));
            Assert.That(job.Mode, Is.EqualTo("Sync"));
            Assert.That(job.Namespace, Is.EqualTo("PluginRight.Plugins"));
            Assert.That(job.Name, Is.EqualTo("MiniTest"));
            Assert.That(job.System, Is.EqualTo("System message for minimal test."));
            Assert.That(job.User, Is.EqualTo("User message for minimal test."));
        }
    }
}

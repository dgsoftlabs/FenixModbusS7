using NUnit.Framework;
using ProjectDataLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.IO;

namespace ProjectDataLib.Tests
{
    [TestFixture()]
    public class ProjectContainerTests
    {
        [Test()]
        public async Task GetVersionFromGitHubTest()
        {
            string content = await ProjectContainer.GetVersionFromGitHub();
            Assert.IsNotNull(content, "Failed to retrieve version content from GitHub.");
        }

        [Test()]
        public void ParseVersionFromContentTest_ValidContent()
        {
            string content = "<?xml version=\"1.0\" encoding=\"utf-8\"?><Fenix><version>3.3.3.0</version><url>https://sourceforge.net/projects/fenixmodbus/</url></Fenix>";
            Version version = ProjectContainer.ParseVersionFromContent(content);
            Assert.IsNotNull(version, "Failed to parse version from valid content.");
            Assert.AreEqual(new Version("3.3.3.0"), version, "Parsed version does not match expected version.");
        }

        [Test()]
        public void ParseVersionFromContentTest_InvalidContent()
        {
            string content = "<?xml version=\"1.0\" encoding=\"utf-8\"?><Fenix><ver>3.3.3.0</ver><url>https://sourceforge.net/projects/fenixmodbus/</url></Fenix>";
            Version version = ProjectContainer.ParseVersionFromContent(content);
            Assert.IsNull(version, "Parsed version should be null for invalid content.");
        }

        [Test()]
        public void ParseUrlFromContentTest_ValidContent()
        {
            string content = "<?xml version=\"1.0\" encoding=\"utf-8\"?><Fenix><version>3.3.3.0</version><url>https://sourceforge.net/projects/fenixmodbus/</url></Fenix>";
            string url = ProjectContainer.ParseUrlFromContent(content);
            Assert.IsNotNull(url, "Failed to parse URL from valid content.");
            Assert.AreEqual("https://sourceforge.net/projects/fenixmodbus/", url, "Parsed URL does not match expected URL.");
        }

        [Test()]
        public void ParseUrlFromContentTest_InvalidContent()
        {
            string content = "<?xml version=\"1.0\" encoding=\"utf-8\"?><Fenix><version>3.3.3.0</version><link>https://sourceforge.net/projects/fenixmodbus/</link></Fenix>";
            string url = ProjectContainer.ParseUrlFromContent(content);
            Assert.IsNull(url, "Parsed URL should be null for invalid content.");
        }

        [Test()]
        public void ProjectXmlSerialization_ShouldSerializeWithoutReflectionErrors()
        {
            var project = new Project();
            var serializer = new XmlSerializer(typeof(Project));

            using var ms = new MemoryStream();
            Assert.DoesNotThrow(() => serializer.Serialize(ms, project));
        }
    }
}
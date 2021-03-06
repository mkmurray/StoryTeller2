using System.IO;
using FubuCore;
using NUnit.Framework;
using StoryTeller.Domain;
using StoryTeller.Engine;
using StoryTeller.Persistence;
using StoryTeller.Workspace;
using FileSystem = FubuCore.FileSystem;
using IFileSystem = FubuCore.IFileSystem;

namespace StoryTeller.Testing.Workspace
{
    [TestFixture]
    public class ProjectTester
    {
        #region Setup/Teardown

        [SetUp]
        public void SetUp()
        {
        }

        #endregion

        [Test]
        public void default_test_folder()
        {
            var project = new Project();
            project.TestFolder.ShouldEqual("Tests");
        }

        [Test]
        public void default_compile_target()
        {
            new Project().CompileTarget.ShouldEqual("debug");
        }

        [Test]
        public void default_binary_folder()
        {
            new Project {ProjectFolder = "foo", CompileTarget = "retail"}.GetBinaryFolder()
                .ShouldEqual(Path.Combine("foo", "bin", "retail").ToFullPath());
        }

        [Test]
        public void use_the_overridden_binary_folder_if_desired()
        {
            new Project {ProjectFolder = "foo", BinaryFolder = "bin"}
                .GetBinaryFolder()
                .ShouldEqual(Path.Combine("foo", "bin").ToFullPath());
        }

        [Test]
        public void use_the_compile_target_to_determine_the_binary_folder()
        {
            new Project { ProjectFolder = "foo", CompileTarget = "release"}.GetBinaryFolder()
                .ShouldEqual(Path.Combine("foo", "bin", "release").ToFullPath());
        }

        [Test]
        public void create_a_directory()
        {
            var project = new Project
            {
                BinaryFolder = string.Empty,
                ProjectFolder = "",
                TestFolder = ""
            };


            if (Directory.Exists("NewSuite")) Directory.Delete("NewSuite", true);
            var suite = new Suite("NewSuite");

            project.CreateDirectory(suite);

            Directory.Exists("NewSuite").ShouldBeTrue();

            var childSuite = new Suite("Child");
            suite.AddSuite(childSuite);

            project.CreateDirectory(childSuite);

            Directory.Exists("NewSuite\\Child").ShouldBeTrue();
        }

        [Test]
        public void delete_a_test_file()
        {
            var project = new Project
            {
                BinaryFolder = string.Empty,
                ProjectFolder = "",
                TestFolder = ""
            };
            var test = new Test("test to be saved");
            test.AddComment("some comment");
            test.FileName = "Test001.xml";

            project.Save(test);

            File.Exists("Test001.xml").ShouldBeTrue();
            project.DeleteFile(test);
            File.Exists("Test001.xml").ShouldBeFalse();
        }


        [Test]
        public void get_test_path()
        {
            var project = new Project(@"c:\a\b\c\d\project.proj")
            {
                TestFolder = "tests"
            };
            var hierarchy = DataMother.BuildHierarchy(@"
t1,Success
s1/t2,Success
s1/s2/t3,Success
");


            var test = hierarchy.FindTest("t1");

            project.GetTestPath(test).ShouldEqual(@"c:\a\b\c\d\tests\t1.xml");
            project.GetTestPath(hierarchy.FindTest("s1/t2")).ShouldEqual(@"c:\a\b\c\d\tests\s1\t2.xml");
            project.GetTestPath(hierarchy.FindTest("s1/s2/t3")).ShouldEqual(@"c:\a\b\c\d\tests\s1\s2\t3.xml");
        }

        [Test]
        public void get_test_path_when_the_test_overrides_the_file_name()
        {
            var project = new Project(@"c:\a\b\c\d\project.proj")
            {
                TestFolder = "tests"
            };
            var hierarchy = DataMother.BuildHierarchy(@"
t1,Success
s1/t2,Success
s1/s2/t3,Success
");

            var test = hierarchy.FindTest("t1");
            test.FileName = "TheBigTest.xml";

            project.GetTestPath(test).ShouldEqual(@"c:\a\b\c\d\tests\TheBigTest.xml");
        }

        [Test]
        public void get_the_test_path_of_a_test_at_the_hierarchy_scope()
        {
            var project = new Project(@"c:\a\b\c\d\project.proj")
            {
                TestFolder = "tests"
            };
            var hierarchy = new Hierarchy("something");
            var test = new Test("t0");
            hierarchy.AddTest(test);
            project.GetTestPath(test).ShouldEqual(@"c:\a\b\c\d\tests\t0.xml");
        }

        [Test]
        public void GetBaseFolder()
        {
            var project = new Project(@"c:\a\b\c\d\project.proj");
            Assert.AreEqual(@"c:\a\b\c\d", project.GetBaseProjectFolder());
        }

        [Test]
        public void GetBaseFolderReturnsEmptyStringIfNoFileNameIsSet()
        {
            var project = new Project();
            Assert.AreEqual(string.Empty, project.GetBaseProjectFolder());
        }

        [Test]
        public void save_and_load_a_test()
        {
            var project = new Project
            {
                BinaryFolder = string.Empty,
                ProjectFolder = "",
                TestFolder = ""
            };
            var test = new Test("test to be saved");
            test.AddComment("some comment");
            test.FileName = "Test001.xml";

            project.Save(test);

            var test2 = new TestReader().ReadFromFile(test.FileName);
            test2.Name.ShouldEqual(test.Name);
            test2.Parts.Count.ShouldEqual(1);

            test2.FileName.ShouldEqual("Test001.xml");
        }
    }

    [TestFixture]
    public class when_renaming_a_file
    {
        #region Setup/Teardown

        [SetUp]
        public void SetUp()
        {
            var project = new Project
            {
                BinaryFolder = string.Empty,
                ProjectFolder = "",
                TestFolder = ""
            };
            test = new Test("Test001");
            test.AddComment("some comment");

            project.Save(test);

            project.RenameTest(test, "New Name");
        }

        #endregion

        private Test test;

        [Test]
        public void the_new_file_name_should_reflect_the_new_name()
        {
            test.FileName.ShouldEqual("New_Name.xml");
        }

        [Test]
        public void the_new_test_name_should_be_set()
        {
            test.Name.ShouldEqual("New Name");
        }

        [Test]
        public void the_old_file_should_be_deleted()
        {
            File.Exists("Test001.xml").ShouldBeFalse();
        }

        [Test]
        public void the_test_should_now_be_saved_at_the_new_file_location()
        {
            File.Exists("New_Name.xml");

            var test2 = new TestReader().ReadFromFile("New_Name.xml");
            test2.Parts[0].ShouldBeOfType<Comment>().Text.ShouldEqual("some comment");
        }
    }

    [TestFixture]
    public class when_determining_the_config_file_if_none_specified
    {
        private IFileSystem fileSystem = new FileSystem();

        [SetUp]
        public void SetUp()
        {
            fileSystem.DeleteDirectory("Foo");
        }

        [Test]
        public void use_app_config_if_it_exists()
        {
            fileSystem.WriteStringToFile("Foo".AppendPath("App.config"), "anything");

            Project.ForDirectory("Foo")
                .ConfigurationFileName.ShouldEqual("Foo".AppendPath("App.config").ToFullPath());
        }

        [Test]
        public void use_app_config_if_it_exists_2()
        {
            fileSystem.WriteStringToFile("Foo".AppendPath("app.config"), "anything");

            Project.ForDirectory("Foo")
                .ConfigurationFileName.ShouldEqual("Foo".AppendPath("app.config").ToFullPath());
        }

        [Test]
        public void use_web_config_if_it_exists()
        {
            fileSystem.WriteStringToFile("Foo".AppendPath("Web.config"), "anything");

            Project.ForDirectory("Foo")
                .ConfigurationFileName.ShouldEqual("Foo".AppendPath("Web.config").ToFullPath());
        }

        [Test]
        public void use_web_config_if_it_exists_2()
        {
            fileSystem.WriteStringToFile("Foo".AppendPath("web.config"), "anything");

            Project.ForDirectory("Foo")
                .ConfigurationFileName.ShouldEqual("Foo".AppendPath("web.config").ToFullPath());
        }

        [Test]
        public void use_assembly_name_dll_config_file_if_it_exists()
        {
            fileSystem.WriteStringToFile("Foo".AppendPath("Foo.dll.config"), "anything");

            Project.ForDirectory("Foo").ConfigurationFileName
                .ShouldEqual("Foo".AppendPath("Foo.dll.config").ToFullPath());
        }
    }
}

namespace StateFixtures
{
    public class OhioFixture : Fixture
    {
    }

    public class WisconsinFixture : Fixture
    {
    }

    public class IllinoisFixture : Fixture
    {
    }
}

namespace DirectionFixtures
{
    public class NorthFixture : Fixture
    {
    }

    public class SouthFixture : Fixture
    {
    }
}
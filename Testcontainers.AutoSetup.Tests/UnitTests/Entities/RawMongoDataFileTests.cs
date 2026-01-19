using Testcontainers.AutoSetup.Core.Common.Entities;
using Testcontainers.AutoSetup.Core.Common.Enums;

namespace Testcontainers.AutoSetup.Tests.UnitTests.Entities;

public class RawMongoDataFileTests
{
    [Fact]
    public async Task Ctor_ThrowsArgumentException_IfFileArrayFlagSetForNotJsonFile()
    {
        // Arrange & Act
        var ex = await Assert.ThrowsAsync<ArgumentException>(async () => 
        new RawMongoDataFile(
            collectionName: "tst", fileName: "test", 
            extension: MongoDataFileExtension.CSV, isJsonArray: true));

        //Assert
        Assert.Equal("JSON array param must be used only with JSON files.", ex.Message);
    }

    [Theory]
    [InlineData("testColl1", "testFile1", MongoDataFileExtension.CSV, false)]
    [InlineData("testColl2", "testFile2", MongoDataFileExtension.JSON, false)]
    [InlineData("testColl3", "testFile3", MongoDataFileExtension.JSON, true)]
    public async Task Ctor_InitializesCorrectValues(string collectionName,string fileName, MongoDataFileExtension extension, bool isJsonArray)
    {
        // Arrange & Act
        var mongoFile = new RawMongoDataFile(collectionName, fileName, extension, isJsonArray);

        //Assert
        Assert.Equal(collectionName, mongoFile.CollectionName);
        Assert.Equal(fileName, mongoFile.FileName);
        Assert.Equal(extension, mongoFile.FileExtension);
        Assert.Equal(isJsonArray, mongoFile.IsJsonArray);
    }
}

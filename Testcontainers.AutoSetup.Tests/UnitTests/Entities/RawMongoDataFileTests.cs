using Testcontainers.AutoSetup.Core.Common.Entities;

namespace Testcontainers.AutoSetup.Tests.UnitTests.Entities;

public class RawMongoDataFileTests
{
    [Theory]
    [InlineData("testColl2", "testFile2", false)]
    [InlineData("testColl3", "testFile3", true)]
    public async Task Ctor_InitializesCorrectValues_IfJsonFile(string collectionName,string fileName, bool isJsonArray)
    {
        // Arrange & Act
        var mongoFile = RawMongoDataFile.FromJson(collectionName, fileName, isJsonArray);

        //Assert
        Assert.Equal(collectionName, mongoFile.CollectionName);
        Assert.Equal(fileName, mongoFile.FileName);
        Assert.Equal(isJsonArray, mongoFile.IsJsonArray);
        Assert.Null(mongoFile.CsvImportFlag);
    }

    [Fact]
    public async Task Ctor_InitializesCorrectValues_IfCsv()
    {
        // Arrange & Act
        const string collectionName = "collectionName";
        const string fileName = "fileName";
        const string csvImportParams = "--headerline";
        var mongoFile = RawMongoDataFile.FromCsvWithHeaderfileFlag(collectionName, fileName);

        //Assert
        Assert.Equal(collectionName, mongoFile.CollectionName);
        Assert.Equal(fileName, mongoFile.FileName);
        Assert.Equal(csvImportParams, mongoFile.CsvImportFlag);
        Assert.Null(mongoFile.IsJsonArray);
    }
}

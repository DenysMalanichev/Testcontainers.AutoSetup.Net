using Testcontainers.AutoSetup.Core.Common;
using Testcontainers.AutoSetup.Core.Common.Entities;
using Testcontainers.AutoSetup.Core.Common.Enums;

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

    [Fact]
    public void FromCsvWithFieldsFlag_InitializesCorrectValues_WhenValid()
    {
        // Arrange
        const string collectionName = "users";
        const string fileName = "users_no_header";
        const string fields = "_id,username,email";

        // Act
        var mongoFile = RawMongoDataFile.FromCsvWithFieldsFlag(collectionName, fileName, fields);

        // Assert
        Assert.Equal(collectionName, mongoFile.CollectionName);
        Assert.Equal(fileName, mongoFile.FileName);
        Assert.Equal(MongoDataFileExtension.CSV, mongoFile.FileExtension);
        Assert.Equal("--fields", mongoFile.CsvImportFlag);
        Assert.Equal(fields, mongoFile.CsvImportParams);
        Assert.Null(mongoFile.IsJsonArray);
    }

    [Fact]
    public void FromCsvWithFieldFileFlag_InitializesCorrectValues_AndConstructsFullPath()
    {
        // Arrange
        const string collectionName = "products";
        const string fileName = "products_no_header";
        const string fieldFileName = "products_headers.txt";
        
        // Expected path construction based on your record logic
        var expectedFullPath = $"{Constants.MongoDB.DefaultMigrationsDataPath}/{fieldFileName}";

        // Act
        var mongoFile = RawMongoDataFile.FromCsvWithFieldFileFlag(collectionName, fileName, fieldFileName);

        // Assert
        Assert.Equal(collectionName, mongoFile.CollectionName);
        Assert.Equal(fileName, mongoFile.FileName);
        Assert.Equal(MongoDataFileExtension.CSV, mongoFile.FileExtension);
        Assert.Equal("--fieldFile", mongoFile.CsvImportFlag);
        
        Assert.Equal(expectedFullPath, mongoFile.CsvImportParams); 
    }

    [Theory]
    [InlineData("", "file", "fields")] // Empty Collection
    [InlineData("coll", "", "fields")] // Empty FileName
    [InlineData("coll", "file", "")]   // Empty Fields/FieldFile
    public void FactoryMethods_ShouldThrowArgumentException_WhenParamsEmpty(string coll, string file, string param)
    {
        Assert.Throws<ArgumentException>(() => 
            RawMongoDataFile.FromCsvWithFieldsFlag(coll, file, param));

        Assert.Throws<ArgumentException>(() => 
            RawMongoDataFile.FromCsvWithFieldFileFlag(coll, file, param));
            
        // Assert - FromCsvWithHeaderfileFlag (only checks coll and file)
        if (string.IsNullOrEmpty(coll) || string.IsNullOrEmpty(file))
        {
             Assert.Throws<ArgumentException>(() => 
                RawMongoDataFile.FromCsvWithHeaderfileFlag(coll, file));
        }
    }
}

using Testcontainers.AutoSetup.Core.Common.Enums;

namespace Testcontainers.AutoSetup.Core.Common.Entities;

public record RawMongoDataFile
{
    public string CollectionName { get; init; }
    public string FileName { get; init; }
    public MongoDataFileExtension FileExtension { get; init; }

    public bool? IsJsonArray { get; init; }

    public string? CsvImportFlag { get; init; }
    public string? CsvImportParams { get; init; }

    private RawMongoDataFile(
        string collectionName, 
        string fileName, 
        MongoDataFileExtension extension,
        bool? isJsonArray,
        string? csvImportFlag,
        string? csvImportParams)
    {
        CollectionName = collectionName;
        FileName = fileName;
        FileExtension = extension;
        IsJsonArray = isJsonArray;
        CsvImportFlag = csvImportFlag;
        CsvImportParams = csvImportParams;
    }

    /// <summary>
    /// Creates a setup for a JSON file.
    /// </summary>
    public static RawMongoDataFile FromJson(
        string collectionName, 
        string fileName, 
        bool isJsonArray = false)
    {
        return new RawMongoDataFile(
            collectionName, 
            fileName, 
            MongoDataFileExtension.JSON, 
            isJsonArray, 
            null,
            null
        );
    }

    /// <summary>
    /// Creates a setup for a CSV file with --headerline flag.
    /// </summary>
    public static RawMongoDataFile FromCsvWithHeaderfileFlag(string collectionName, string fileName)
    {
        ArgumentException.ThrowIfNullOrEmpty(collectionName);
        ArgumentException.ThrowIfNullOrEmpty(fileName);

        return new RawMongoDataFile(
            collectionName, 
            fileName, 
            MongoDataFileExtension.CSV, 
            null,
            "--headerline",
            null
        );
    }

    /// <summary>
    /// Creates a setup for a CSV file with --fields flag.
    /// </summary>
    public static RawMongoDataFile FromCsvWithFieldsFlag(string collectionName, string fileName, string fields)
    {
        ArgumentException.ThrowIfNullOrEmpty(collectionName);
        ArgumentException.ThrowIfNullOrEmpty(fileName);
        ArgumentException.ThrowIfNullOrEmpty(fields);

        return new RawMongoDataFile(
            collectionName, 
            fileName, 
            MongoDataFileExtension.CSV, 
            null,
            "--fields",
            fields
        );
    }

    /// <summary>
    /// Creates a setup for a CSV file with --fields flag.
    /// </summary>
    public static RawMongoDataFile FromCsvWithFieldFileFlag(string collectionName, string fileName, string fieldFileName)
    {
        ArgumentException.ThrowIfNullOrEmpty(collectionName);
        ArgumentException.ThrowIfNullOrEmpty(fileName);
        ArgumentException.ThrowIfNullOrEmpty(fieldFileName);
        
        var fullFileName = $"{Constants.MongoDB.DefaultMigrationsDataPath}/{fieldFileName}";

        return new RawMongoDataFile(
            collectionName, 
            fileName, 
            MongoDataFileExtension.CSV, 
            null,
            "--fieldFile",
            fullFileName
        );
    }
}
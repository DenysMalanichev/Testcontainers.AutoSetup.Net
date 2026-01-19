using Testcontainers.AutoSetup.Core.Common.Enums;

namespace Testcontainers.AutoSetup.Core.Common.Entities;

public record RawMongoDataFile
{
    /// <summary>
    /// A name of the MongoDB collection where data will be inserted
    /// </summary>
    public string CollectionName { get; init; }

    /// <summary>
    /// A name of the file without extension
    /// </summary>
    public string FileName { get; init; }

    /// <summary>
    /// File extension
    /// </summary>
    public MongoDataFileExtension FileExtension { get; init; }

    /// <summary>
    /// Must be set to True if a json file is intended to be treated as array
    /// </summary>
    public bool IsJsonArray { get; init; } = false;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="collectionName">A name of the MongoDB collection where data will be inserted</param>
    /// <param name="fileName">A name of the file without extension</param>
    /// <param name="extension"><see cref="MongoDataFileExtension"/> file extension</param>
    /// <param name="isJsonArray">Optional param defaulting to false. Must be set to True if a json file is intended to be treated as array</param>
    /// <exception cref="ArgumentException"></exception>
    public RawMongoDataFile(
        string collectionName,
        string fileName,
        MongoDataFileExtension extension,
        bool isJsonArray = false)
    {
        CollectionName = collectionName;
        FileName = fileName;
        FileExtension = extension;
        IsJsonArray = isJsonArray;

        if(isJsonArray && FileExtension is not MongoDataFileExtension.JSON)
        {
            throw new ArgumentException("JSON array param must be used only with JSON files.");
        }
    }
}

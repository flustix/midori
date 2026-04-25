namespace Midori.Database.MongoDB;

public class MongoConfig
{
    public string Connection { get; set; }
    public string Database { get; set; }

    public MongoConfig(string connection, string database)
    {
        Connection = connection;
        Database = database;
    }
}

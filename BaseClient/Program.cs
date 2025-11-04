using BaseClient;


public  class Program 
{

    public static async Task Main(string[] args) 
    {
        Client c = new Client();
        await c.RunAsync();
    
    }
}
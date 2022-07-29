
if (args.Length != 3 && args.Length != 4)
{
    ShowHelp();
}
else
{
    try
    {
        var formatName = args[0];
        var format = FormatManager.GetFormat(formatName);

        bool isEncrypt = false;
        if (args.Length == 4 && args[1] == "e")
            isEncrypt = true;

        var input = args[^2];
        var output = args[^1];
        format.Process(input, output, isEncrypt);
    }
    catch(NotImplementedException)
    {
        Console.WriteLine("This feature is not supported yet !!");
    }
    catch(Exception e)
    {
        Console.WriteLine(e.Message);
        Console.ReadKey();
    }
}

void ShowHelp()
{
    var versionString = Assembly.GetEntryAssembly()?
                                            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                                            .InformationalVersion
                                            .ToString();

    Console.WriteLine(@$"MiHoYoAssets v{versionString}
------------------------
Usage:
    MiHoYoAssets format [e] input_path output_path

Available formats:
{FormatManager.GetFormats()} 

Decrypt is default, Set 'e' only when encrypt.");
}
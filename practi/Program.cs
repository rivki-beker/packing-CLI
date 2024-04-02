using System.CommandLine;

var rootCommand = new RootCommand("A command-line app to zip files");

var bundleCommand = new Command("bundle", "A command to zip files");

var languageOption = new Option<string[]>("--language", "An option to define the languages of the files to zip. enter the ending of them")
{ IsRequired = true };
languageOption.AllowMultipleArgumentsPerToken = true;
languageOption.AddAlias("-l");

var outputOption = new Option<string>("--output", "An option to define the name and the location of the extract file");
outputOption.AddAlias("-o");

var noteOption = new Option<bool>("--note", "An option to define that the source will be written");
noteOption.AddAlias("-n");

var sortOption = new Option<string>("--sort", getDefaultValue: () => "alphabetic", "An option to define the order of the files")
    .FromAmong("alphabetic", "by-type");
sortOption.AddAlias("-s");

var cleanOption = new Option<bool>("--remove-empty-lines", "An option to define removing empty lines");
cleanOption.AddAlias("-r");

var authorOption = new Option<string>("--author", "An option to enter the name of the author");
authorOption.AddAlias("-a");

bundleCommand.AddOption(languageOption);
bundleCommand.AddOption(outputOption);
bundleCommand.AddOption(noteOption);
bundleCommand.AddOption(sortOption);
bundleCommand.AddOption(cleanOption);
bundleCommand.AddOption(authorOption);

rootCommand.AddCommand(bundleCommand);

bundleCommand.SetHandler(async (language, output, note, sort, clean, author) =>
{
    await ZipFiles(language, output, note, sort, clean, author);
},
languageOption, outputOption, noteOption, sortOption, cleanOption, authorOption);

var rspCommand = new Command("create-rsp", "A command to create response file");

rootCommand.AddCommand(rspCommand);

rspCommand.SetHandler(() => CreateRspFile());

await rootCommand.InvokeAsync(args);

static async Task ZipFiles(
    string[] language, string output, bool note, string sort, bool clean, string author)
{
    try
    {
        List<string> filesToConcatenate = new List<string>();

        if (language[0] == "all")
            filesToConcatenate.AddRange(Directory.GetFiles(".\\", "*", SearchOption.AllDirectories));
        else foreach (string lng in language)
                filesToConcatenate.AddRange(Directory.GetFiles(".\\", $"*.{lng}", SearchOption.AllDirectories));

        if (filesToConcatenate.Count == 0)
        {
            Console.WriteLine("No files found to concatenate.");
            return;
        }

        filesToConcatenate.Sort((a, b) => (sort == "alphabetic") ?
            string.Compare(new FileInfo(a).Name, new FileInfo(b).Name) :
            string.Compare(a.Substring(a.LastIndexOf('.')), b.Substring(b.LastIndexOf('.'))));

        output = (output == null) ? "output.txt" : output;

        using (StreamWriter writer = new StreamWriter(output))
        {
            if (author != null)
                writer.WriteLine("//The author is: " + author);
            foreach (string file in filesToConcatenate)
            {
                if (new FileInfo(file).Directory.Name is "bin" or "debug" or "node_modules")
                    continue;

                if (note)
                    await writer.WriteLineAsync($"//The source of the code is- name: {new FileInfo(file).Name}, routing: {file.Substring(0, file.LastIndexOf("\\") + 1)}");

                var lines = (await File.ReadAllLinesAsync(file)).ToList();

                if (clean)
                {
                    lines = lines.Where(arg => !string.IsNullOrWhiteSpace(arg)).ToList();
                    File.WriteAllLines(file, lines);
                }

                foreach (string line in lines)
                    await writer.WriteLineAsync(line);

                await writer.WriteLineAsync();
            }
        }
        Console.WriteLine($"Files successfully concatenated to: {output}");
    }
    catch (DirectoryNotFoundException)
    {
        Console.WriteLine($"Error: The directory: {new FileInfo(output).DirectoryName} not found");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
}

static void CreateRspFile()
{
    using (StreamWriter writer = new StreamWriter("bundle.rsp"))
    {
        writer.WriteLine("bundle");
        Console.Clear();
        Console.WriteLine("Enter a list of file extensions you want to bundle:");
        writer.WriteLine("--language " + Console.ReadLine());
        Console.Clear();
        Console.Write("Bundled file name: ");
        writer.WriteLine("--output " + Console.ReadLine());
        Console.Clear();
        Console.Write("Do you want to add as a note the code source? (Y/N):  ");
        if (Console.ReadLine() is "Y" or "y")
            writer.WriteLine("--note");
        string[] options = { "alphabetic", "by type" };
        int selectedIndex = 0;

        Console.CursorVisible = false;

        bool continueLoop = true;
        while (continueLoop)
        {
            Console.Clear();

            Console.WriteLine("Choose the sort order:");

            for (int i = 0; i < options.Length; i++)
            {
                if (i == selectedIndex)
                {
                    Console.ForegroundColor = ConsoleColor.Magenta;
                }

                Console.WriteLine("> " + options[i]);

                Console.ResetColor();
            }

            ConsoleKeyInfo key = Console.ReadKey();

            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                    selectedIndex = (selectedIndex - 1 + options.Length) % options.Length;
                    break;

                case ConsoleKey.DownArrow:
                    selectedIndex = (selectedIndex + 1) % options.Length;
                    break;

                case ConsoleKey.Enter:
                    if (options[selectedIndex] == "by type")
                        writer.WriteLine("--sort by-type");
                    continueLoop = false;
                    break;

                default:
                    break;
            }
        }
        Console.CursorVisible = true;
        Console.Clear();
        Console.Write("Do you want to remove empty lines? (Y/N):  ");
        if (Console.ReadLine() is "Y" or "y")
            writer.WriteLine("--remove-empty-lines");
        Console.Clear();
        Console.Write("Author name: ");
        writer.WriteLine("--author " + Console.ReadLine());
        Console.Clear();
    }

    Console.WriteLine("The response file was created successfully.");
    Console.WriteLine("Now run: practi @bundle.rsp");
}
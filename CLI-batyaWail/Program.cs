using System.CommandLine;
using System.Text;
using System;
using System.IO;
using static System.Net.WebRequestMethods;
using File = System.IO.File;
using System.Reflection.PortableExecutable;
using Microsoft.VisualBasic.FileIO;
#region bundle

List<string> listLanguage = new List<string>() { "c#", "java", "html", "jsx", "css", "c", "ts", "python" };
List<string> listTypeLanguage = new List<string>() { "cs", "java", "html", "jsx", "css", "c", "ts", "py" };
List<string> userLanguage;
List<string> CorrectUserLanguage = new List<string>();
var bundleCommand = new Command("bundle", "Bundele code files to a single file");
//option
var bundleOpsionOutput = new Option<FileInfo>("--output", "File path and name");
var bundleOptionLanguage = new Option<string>("--language", "list of language or all language") { IsRequired = true };
var bundleOptionNote = new Option<bool>("--note", "write the relative routing of file");
var bundleOptionSort = new Option<OrderBy>("--sort", "sort the file by Alphabetic=0, or FileType=1");
var bundleOptionEmptyLines = new Option<bool>("--remove-empty-lines", "If you want to remove empty lines write true");
var bundelOptionAuthor = new Option<string>("--author", "write the author of txt file");
//alias
bundleOpsionOutput.AddAlias("-o");
bundleOptionLanguage.AddAlias("-l");
bundleOptionNote.AddAlias("-n");
bundleOptionSort.AddAlias("-s");
bundleOptionEmptyLines.AddAlias("-r");
bundelOptionAuthor.AddAlias("-a");

bundleCommand.AddOption(bundleOpsionOutput);
bundleCommand.AddOption(bundleOptionLanguage);
bundleCommand.AddOption(bundleOptionNote);
bundleCommand.AddOption(bundleOptionSort);
bundleCommand.AddOption(bundleOptionEmptyLines);
bundleCommand.AddOption(bundelOptionAuthor);

bundleCommand.SetHandler((output, languge, note, sort, emptyLines, author) =>
{
    string fileTxtPath = output.FullName;
    string folderPath = output.FullName;
    string parentFolderPath = Path.GetDirectoryName(folderPath);
    string existingFile = output.Name;
    OrderBy orderBy = OrderBy.Alphabetic; // Default to alphabetical order
    if (sort == OrderBy.FileType)
    {
        orderBy = OrderBy.FileType;
    }
    //languges
    if (languge == "all")
    {
        CorrectUserLanguage = listTypeLanguage;
    }
    else
    {
        userLanguage = languge.Split().ToList();
        for (int i = 0; i < listLanguage.Count; i++)
        {
            if (userLanguage.Contains(listLanguage[i]))
            {
                CorrectUserLanguage.Add(listTypeLanguage[i]);
            }
        }
        for (int i = 0; i < userLanguage.Count; i++)
        {
            if (!listLanguage.Contains(userLanguage[i]))
            {
                Console.WriteLine("language '" + userLanguage[i] + "' is not correct");
            }
        }
    }
    try
    {
        // Check if file already exists. If yes, delete it.
        if (File.Exists(fileTxtPath))
        {
            File.Delete(fileTxtPath);
        }
        // Create a new file
        using (StreamWriter myTxtFile = File.CreateText(fileTxtPath))
        {
            if (author != null)
            {
                myTxtFile.WriteLine("/* the file created by: " + author + "*/");
            }
            if (note == true)
            {
                myTxtFile.WriteLine("/* the path: " + fileTxtPath + " */");
                myTxtFile.WriteLine("/* the fileName: " + existingFile + " */");
            }
            try
            {
                MergeFiles(parentFolderPath, myTxtFile, orderBy, CorrectUserLanguage, emptyLines);
                Console.WriteLine($"Content merged and saved to {existingFile}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");

            }
        }
    }
    catch (Exception Ex)
    {
        Console.WriteLine(Ex.ToString());
    }

}, bundleOpsionOutput, bundleOptionLanguage, bundleOptionNote, bundleOptionSort, bundleOptionEmptyLines, bundelOptionAuthor);

static void MergeFiles(string folderPath, StreamWriter myTxtFile, OrderBy orderBy, List<string> correctUserLanguage, bool emptyLines)
{
    if (!Directory.Exists(folderPath))
    {
        throw new DirectoryNotFoundException($"The specified folder does not exist: {folderPath}");
    }
    try
    {
        var sortedFiles = GetSortedFiles(folderPath, orderBy);
        foreach (string filePath in sortedFiles)
        {
            string fileName = Path.GetFileName(filePath);
            string fileType = Path.GetExtension(fileName).TrimStart('.');
            if (correctUserLanguage.Contains(fileType))
            {
                myTxtFile.WriteLine($"\n\n\n=== {fileName} ===\n\n\n");
                using (StreamReader reader = new StreamReader(filePath))
                {
                    if (!emptyLines)
                        myTxtFile.Write(reader.ReadToEnd());
                    else
                    {
                        string line;
                        // Read lines from the input file
                        while ((line = reader.ReadLine()) != null)
                        {
                            // Check if the line is not empty
                            if (!string.IsNullOrWhiteSpace(line))
                                myTxtFile.WriteLine(line);
                        }
                    }
                }
            }
        }
        foreach (string subFolderPath in Directory.GetDirectories(folderPath))
        {
            MergeFilesRecursively(subFolderPath, myTxtFile, orderBy, correctUserLanguage, emptyLines);
        }
    }
    catch (Exception ex)
    {
        throw new IOException($"An error occurred while merging files: {ex.Message}");
    }
}
static void MergeFilesRecursively(string folderPath, StreamWriter myTxtFile, OrderBy orderBy, List<string> correctUserLanguage, bool emptyLines)
{
    var sortedFiles = GetSortedFiles(folderPath, orderBy);
    foreach (string filePath in sortedFiles)
    {
        string fileName = Path.GetFileName(filePath);
        string fileType = Path.GetExtension(fileName).TrimStart('.');
        if (correctUserLanguage.Contains(fileType))
        {
            myTxtFile.WriteLine($"\n\n\n=== {fileName} ===\n\n\n");

            using (StreamReader reader = new StreamReader(filePath))
            {
                if (!emptyLines)
                    myTxtFile.Write(reader.ReadToEnd());
                else
                {
                    string line;
                    // Read lines from the input file
                    while ((line = reader.ReadLine()) != null)
                    {
                        // Check if the line is not empty
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            myTxtFile.WriteLine(line);
                        }
                    }
                }
            }
        }
    }
    foreach (string subFolderPath in Directory.GetDirectories(folderPath))
    {
        MergeFilesRecursively(subFolderPath, myTxtFile, orderBy, correctUserLanguage, emptyLines);
    }
}

static IEnumerable<string> GetSortedFiles(string folderPath, OrderBy orderBy)
{
    var files = Directory.GetFiles(folderPath);
    switch (orderBy)
    {
        case OrderBy.Alphabetic:
            return files.OrderBy(file => file, StringComparer.OrdinalIgnoreCase);

        case OrderBy.FileType:
            return files.OrderBy(file => Path.GetExtension(file), StringComparer.OrdinalIgnoreCase);

        default:
            throw new ArgumentException("Invalid order by option.");
    }
}
#endregion
#region create-rsp
var createRspCommand = new Command("create-rsp", "create rsp file with user commands");
createRspCommand.SetHandler(() =>
{
    Console.WriteLine("choose name of response file");
    string fileName = Console.ReadLine();
    Console.WriteLine("Enter name to txt file");
    string txtFileName = Console.ReadLine();
    while (txtFileName == null)
    {
        Console.WriteLine("you must enter name to txt file");
        txtFileName = Console.ReadLine();
    }
    txtFileName = txtFileName + ".txt";
    Console.WriteLine(txtFileName);
    Console.WriteLine("you can choose language one or more  from this list: \"c#\", \"java\", \"html\", \"jsx\", \"css\", \"c\", \"ts\", \"python\"  or write all");
    string language = Console.ReadLine();
    while (language == null)
    {
        Console.WriteLine("this option is required!!!");
        Console.WriteLine("you can choose language one or more  from this list: \"c#\", \"java\", \"html\", \"jsx\", \"css\", \"c\", \"ts\", \"python\"  or write all");
        language = Console.ReadLine();
    }
    Console.WriteLine("do you want to write on the file the path file and name file? (true/false)");
    //bool isNote = bool.Parse(Console.ReadLine());
    string isNote = Console.ReadLine();
    while (isNote != "true" && isNote != "false")
    {
        Console.WriteLine("i-corect choice, pleas write true/false!");
        isNote = Console.ReadLine();
    }
    Console.WriteLine("how do you want to order by al files inside the txt file? by Alphabetic prees 0, by FileType press 1");
    int orderBy = int.Parse(Console.ReadLine());
    Console.WriteLine("do you want to remove empty lines on txt file? (true/false)");
    string isRemove = Console.ReadLine();
    while (isRemove != "true" && isRemove != "false")
    {
        Console.WriteLine("i-corect choice, pleas write true/false!");
        isRemove = Console.ReadLine();
    }
    Console.WriteLine("if do you want - you can write your name (-not requried)");
    string author = Console.ReadLine();
    try
    {
        FileInfo filePath = new FileInfo(fileName + ".rsp");

        using (StreamWriter myRspFile = new StreamWriter(filePath.FullName))
        {
            myRspFile.Write("--output " + txtFileName);
            myRspFile.Write(" --language " + language);
            myRspFile.Write(" --note " + bool.Parse(isNote));
            myRspFile.Write(" --sort " + orderBy);
            myRspFile.Write(" --remove-empty-lines " + bool.Parse(isRemove));
            myRspFile.Write(" --author " + author);
        }

        Console.WriteLine($"RSP file created at: {filePath.FullName}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An error occurred: {ex.Message}");
    }
});
#endregion
var rootCommand = new RootCommand("Root command for file Bundler CLI");
rootCommand.AddCommand(bundleCommand);
rootCommand.AddCommand(createRspCommand);
rootCommand.InvokeAsync(args);
enum OrderBy
{
    Alphabetic,
    FileType
};
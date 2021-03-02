
This application is developed with: Visual Studio 19, .Net Core, C#
So that's what should be used to run it, if running in debug mode.

Install Newtonsoft.Json NuGet package in your Visual Studio 19.
That's the only external library needed to run the application.

Place the following files in C:\temp\NetNationsFiles directory: Sample_Report.csv, typemap.json
Alternative is to change the file paths in the source code matching with where ever you put the files.

Output insert query files named chargeable_insert_query.txt and domains_insert_query.txt
are outputted to folder C:\temp\NetNationsFiles.
If you want to change the location, please edit the location in the source code.

As a backup, I have put the output files generated during my run, and named them as:
chargeable_insert_query_saved.txt, domains_insert_query_saved.txt

If not running from Visual Studio, the program can be run by double clicking on the executable UsageTranslator.exe
But the input files should be in right folder as specified above i.e C:\temp\NetNationsFiles directory


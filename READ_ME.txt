How to get the program running.
-------------------------------

The WindowsExecutable folder has the final .exe
and a required .dll .  No installation is required.
Drop them in any folder and run the .exe .
To "uninstall" remove those two files and the
"FileConvertState.txt" file in the same folder.
No evidence of the program will remain.

To rebuild the .exe in Visual Studio
------------------------------------

Run the Visual Studio solution, and you should
see 3 errors, all related to RestSharp.

Go to Tools/NuGetPackageManager/Manage NuGet Packages for Solution
- Browse for RestSharp
- click on the RestSharp line
- click on the checkbox for Project
- click install

The errors are still listed, but recompiling makes them disappear.
All you really need do is press "Start".

License and allowed use
-----------------------

The overall program is (C) Copyright 2020 by Mark V. Tollefson
with the MIT license.
The BetterFolderBrowser was developed by Willy Kimura
and has the MIT license.

Feel free to use any part, such as the utility methods
(with the exception of the BetterFolderBrowser),
as part of any other project without reference to the
copyright nor the MIT license.

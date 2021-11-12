# GridTimeoutRepro
A little program that puts some load on a v4 Selenium grid. Based on .Net 5 and tested on Windows 10 and WSL2/Ubuntu 20.04. 

Run `dotnet build` to restore

To start the program, navigate to the project folder (with \*.csproj file) and run `dotnet run http://my_v4_selenium_grid_url`. By default this creates 50 concurrent sessions that does stuff to create load on the grid.

Note that if the Selenium dependencies are downgraded to v3 and the program is run against a v3 grid and your OS is Windows, it's likely necessary to tweak your dynamic tcp port range as well as the TcpTimedWaitDelay for closed connections as detailed here: https://docs.microsoft.com/en-us/biztalk/technical-guides/settings-that-can-be-modified-to-improve-network-performance. Otherwise port exhaustion is a likely scenario when running this 😬

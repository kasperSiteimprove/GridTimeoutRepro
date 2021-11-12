# GridTimeoutRepro
A little program that puts some load on a v4 Selenium grid

Run `dotnet build` to restore

To start navigate to project folder (with \*.csproj file) and run `dotnet run http://my_v4_selenium_grid_url`. By default this creates 50 concurrent sessions that does stuff to create load on the grid.

# blockland-log-players
This add-on logs the players that you meet in-game within Blockland. Various information is recorded about these players such as their name history and when you last saw them.

You can find the log files in: `config/client/logs/idlog` in the game's directory.

# Development Notice
This add-on has not been updated or maintained since 2016. As a result, several features no-longer work, including the GUI.

The original goal of this project was to provide a decentralized alternative to the RTB ID lookup website. Multiple alternatives to the ID lookup site now exist, available at these sites:
- http://blid.daprogs.com/blid_tool/
- http://mods.greek2me.us/statistics/

This project has been archived as of March 21, 2019. The code is freely available for use under the MIT license.

# Features
- Records name changes of players. (Also logs when each change was first seen)
- Keeps track of the highest ID you've seen.
- Allows you to search for players by name.

# Console Commands
`echo($IDLogger::HighestID);` - Shows the highest recorded ID

`$IDLogger::NoExport = 1;` - Disables automatic exporting

`echo(getFileCount("config/client/logs/idlog/ids/*"));` - Shows the total number of players that you have logged (Only counts exported IDs)

`echo(IDLogger_getTotalNameChanges()-getFileCount("config/client/logs/idlog/names/*"));` - Shows the total number of name changes that you have logged (Only counts exported IDs)

`IDLogger_Search("NAME");` - Searches the log files for the specified name. The name does not need to be complete or case-sensitive. It will show all matches in the console when the search is finished.

`IDLogger_ViewStats("ID");` - Shows information about the specified ID.

`IDLogger_ViewNameHistory("ID");` - Shows the name history for the specified ID.

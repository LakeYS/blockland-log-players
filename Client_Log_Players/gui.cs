//awake checks temp disabled until they are fully implemented
$IDLogger::DlgVersion = 1;

function IDLogger_InitUI()
{
	if(!isObject(statsPane_Players))
	{
		if(!isObject(statsGui) || $IDLogger::DlgVersion > statsGui.version) // Need to create the base GUI (Doesn't exist or uses older version)
		{
			if(isObject(statsGui))
				statsGui.delete();
			
			exec("./gui_stats.cs"); // Load the base GUI
			statsGui.version = $IDLogger::DlgVersion;
		}
		
		exec("./statsPane_Players.gui"); // Load the player tab
		exec("./statsGui_PLB.gui"); // Load the message box
		statsGui_Window.add(statsPane_Players);
		
		//the for loop causes it? replaced 5 with statsgui_window.getCount()
		for(%i = 1; %i <= statsgui_window.getCount(); %i++) // Add a button for the tab
		{
			%obj = "statsGui_Button" @ %i;
			if(isObject(%obj))
			{
				%obj.command = "statsGui.setPane(\"Players\");";
				%obj.setText("Players");
				%obj.setVisible(1);
				%obj.setName("statsGui_ButtonPlayers");
				break;
			}
		}
	}
}

$IDLogger::SearchList = statsGui_PL_SearchList;

// StatsGui stuff
function StatsGui_PL_ClickRefresh()
{
	%val = statsGui_PL_SearchText.getValue();
	
	statsGui.sort = -1;
	statsGui_PL_SearchList.clear();
	statsGui_PL_SearchListFiltered.clear();
	
	IDLogger_SearchFiles("",0,0,"if(%name $= \"\") { echo(%i @ \"BLANK\"); } StatsGui_PL_UpdateList(1,%inc,%name,%blid,%seen,%history);",1,1,"StatsGui_PL_UpdateList(0,%inc);");
}

function statsGui_PL_SearchStart()
{
	statsGui_PL_BtnRefresh.text = "Cancel";
	statsGui_PL_BtnRefresh.command = "cancel($IDLogger::SearchLoop); StatsGui_PL_SearchStop();";
	statsGui_PL_Progress.setVisible(1);
	
}

function statsGui_PL_SearchStop()
{
	statsGui_PL_BtnRefresh.text = "Refresh";
	statsGui_PL_BtnRefresh.command = "statsGui_PL_ClickRefresh(); StatsGui_PL_SearchStart();";
	statsGui_PL_Progress.setVisible(0);
}

function statsGui_PL_UpdateList(%addRow,%i,%name,%blid,%seen,%history)
{
	//%awake = statsGui.isAwake() && statsPane_Players.visible;
	%awake = 1;
	
	if(%addRow)
		statsGui_PL_SearchList.addRow(%i,%name TAB %blid TAB StatsGui_sortDate(%seen,1,1) SPC %blid); // Add to the unfiltered list
	
	if(%i >= $IDLogger::FileCount)
		StatsGui_PL_SearchStop();
	
	statsGui_PL_Progress.setValue(%i / $IDLogger::FileCount); // Update the progress bar
	
	if(%history)
		%color = "\c4";
	
	%val = statsGui_PL_SearchText.getValue(); // If we're searching for something, filter it
	if(%val !$= "" && %addRow) 
	{
		%search = strstr(strupr(%name),strupr(%val));
		if(%search >= 0)
			statsGui_PL_SearchListFiltered.addRow(%i,%color @ %name TAB %blid TAB %seen SPC %blid TAB %search SPC %blid);
		
		if(%awake)
			statsGui_PL_SearchListFiltered.sort(3,1);
	}
	
	if(statsGui.sort != -1 && %awake) // Sort the list
	{
		%list = statsGui.sort;
		if(%list == 0)
			$IDLogger::SearchList.sort(0,statsGui.sortOrder);
		else if(%list == 1)
			$IDLogger::SearchList.sortNumerical(%list,statsGui.sortOrder);
		else
			$IDLogger::SearchList.sort(2,statsGui.sortOrder);
	}
}

function statsGui_PL_FilterList()
{
	%val = statsGui_PL_SearchText.getValue();
	//%awake = statsGui.isAwake() && statsPane_Players.visible;
	%awake = 1;
	
	if(%val $= "") // If the box is empty, we'll just show the unfiltered list.
	{
		$IDLogger::SearchList = statsGui_PL_SearchList;
		
		statsGui_PL_SearchList.setVisible(1); // Set the active list as visible
		statsGui_PL_SearchListFiltered.setVisible(0);
		statsGui.add(statsGui_PL_SearchListFiltered); // Move the other list so it doesn't affect the scroll bar
		statsGui_PL_Scroll.add(statsGui_PL_SearchList);
	}
	else
	{
		// Same as above
		$IDLogger::SearchList = statsGui_PL_SearchListFiltered;
		
		statsGui_PL_SearchList.setVisible(0);
		statsGui_PL_SearchListFiltered.setVisible(1);
		statsGui_PL_Scroll.add(statsGui_PL_SearchListFiltered);
		statsGui.add(statsGui_PL_SearchList);
		
		statsGui_PL_SearchListFiltered.clear(); // Clear the temporary items from the list
		
		%count = statsGui_PL_SearchList.rowCount();
		//if(%count > 500) %count = 500;
		
		for(%i = 1; %i <= %count; %i++) // Filter the list to search for the specified string
		{
			//if(%i >= 500) break;
			%rowText = statsGui_PL_SearchList.getRowTextById(%i);
			%search = strstr(strupr(%rowText),strupr(%val));
			if(%search >= 0)
				statsGui_PL_SearchListFiltered.addRow(%i,%rowText TAB %search SPC getField(%rowText,1));
		}
		
		if(%awake)
			statsGui_PL_SearchListFiltered.sort(3,1);
	}
}

function statsGui_PL_ClickName()
{
	%val = $IDLogger::SearchList.getRowTextById($IDLogger::SearchList.getSelectedID());
	
	canvas.pushDialog("statsGui_PLB");
	
	statsGui_PLB_IdText.setText(getField(%val,1));
	statsGui_PLB_NameText.setText(getField(%val,0));
	statsGui_PLB_SeenText.setText(getSubStr(getField(%val,2),0,16));
	
	statsGui_PLB_HistoryList.clear();
	
	// Load the specified ID's name history
	%path = "config/client/logs/idlog/names/" @ getField(%val,1) @ ".log";
	if(isFile(%path))
	{
		%file = new fileObject();
		%file.openForRead(%path);
		%isNameLine = 0; // IMPORTANT: First line is a date.
		%lines = 0;
		%nameLine = "blank";
		while(!%file.isEOF())
		{
			if(%isNameLine)
			{
				%nameLine = %file.readLine();
				%isNameLine = 0;
				
				statsGui_PLB_HistoryList.addRow(%lines,%nameLine TAB statsGui_sortDate(%dateLine,1,1));
			}
			else
			{
				%dateLine = %file.readLine();
				%isNameLine = 1;
			}
			%lines++;
		}
		%file.close();
		%file.delete();
	}
	else
		statsGui_PLB_HistoryList.addRow(0,"\c2No name history file found.");
}

function statsGui_PL_Clear()
{
	statsGui_PL_SearchList.clear();
	statsGui_PL_SearchListFiltered.clear();
	
	statsGui_PLB_IdText.setText("");
	statsGui_PLB_NameText.setText("");
	statsGui_PLB_SeenText.setText("");
	
	statsGui_PLB.setVisible(0);
}

function statsGui_sortDate(%date,%editYr,%editSec)
{
	//%date (string): string from getDateTime
	%mo = getSubStr(%date,0,2);
	%day = getSubStr(%date,3,2);
	%yr = getSubStr(%date,6,2);
	%hr = getSubStr(%date,9,2);
	%min = getSubStr(%date,12,2);
	%sec = getSubStr(%date,15,2);
	
	//%editYr (bool): move year to start
	if(%editYr)
		%dateNew = "20" @ %yr @ "/" @ %mo @ "/" @ %day;
	else
		%dateNew = %mo @ "/" @ %day @ "/" @ %yr;
	
	//%editSec (bool): remove seconds
	if(%editSec)
		%dateNew = %dateNew @ " " @ %hr @ ":" @ %mo;
	else
		%dateNew = %dateNew @ " " @ %hr @ ":" @ %mo @ ":" @ %sec;
}

function statsGui_PLB_CopyName(%text)
{
	return "[" @ getField(%text,1) @ "]" SPC getField(%text,0);
}

IDLogger_InitUI();
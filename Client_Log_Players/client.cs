//$IDLoggerPref::*
//$StatsGuiPref::*

if(isObject(statsPane_Players))
	deactivatePackage("IDLogger");

exec("./gui.cs");

$IDLoggerPref::DisableEcho = 0;
$IDLoggerPref::NoExport = 0;
$IDLoggerPref::DisablePlaytime = 1;

package IDLogger
{
	function secureClientCmd_ClientJoin(%name,%num,%id,%a,%b,%c,%d,%e,%f)
	{
		Parent::secureClientCmd_ClientJoin(%name,%num,%id,%a,%b,%c,%d,%e,%f);
		IDLogger_PlaytimeStartID(%id);
		IDLogger_LogID(%name,%id);
	}
	
	function secureClientCmd_ClientDrop(%name,%rowID,%a,%b,%c,%d,%e,%f)
	{
		%text = NPL_List.getRowTextById(%rowID); 
		%id = getField(%text,3);
		
		if(!$IDLoggerPref::DisableEcho)
			echo("ID Logger - Dropping client \"" @ %name @ "\". ID: " @ %id);
		
		// Make sure we have the right name.
		if($IDLogger::Name[$IDLogger::SessionID[%id]] !$= %name)
			error("ID Logger - Name does not match! (" SPC %name SPC "(" @ %rowID @ ") !$=" SPC $IDLogger::Name[$IDLogger::SessionID[%id]]);

		IDLogger_PlaytimeEndID(%id);
		Parent::secureClientCmd_ClientDrop(%name,%rowID,%a,%b,%c,%d,%e,%f);
	}
	
	function disconnect(%a)
	{
		%total = $IDLoggerTime::ClientCountTotal;
		
		%i = 0;
		%text = NPL_list.getRowText(%i);
		while(NPL_list.getRowText(%i+1) > 1) // Check the next row instead of the current one (Prevents us from allowing a blank row)
		{
			%id = getField(NPL_list.getRowText(%i),3);
			IDLogger_PlaytimeEndID(%id);
		}
		
		// EXPORT CODE
		if(!$IDLoggerPref::NoExport)
		{
			echo("Exporting ID log");
			%file = new FileObject();
			%file.openForWrite("config/client/logs/idlog/HighestID.log");
			%file.writeLine($IDLogger::HighestID);
			%file.writeLine($IDLogger::VersionExport);

			%file.close();
			
			for(%i = 1; %i <= $IDLogger::TotalSessionIDs; %i++)
			{
				$IDLogger::IDsExported++;
				$IDLogger::IDsExportedTotal++;
			
				%ID = $IDLogger::ID[%i];
				%Name = $IDLogger::Name[%i];
				%Seen = $IDLogger::Seen[%i];
				
				%file.openForRead("config/client/logs/idlog/ids/" @ %ID @ ".log");
				%linesRead = 0;
				while(!%file.isEOF() && %linesRead < 3)
				{
					%line = %file.readLine();
					%linesRead++;
					
					if(%linesRead == 3 && getWord(%line,0) $= "Playtime:") //Playtime
						%playtimeFile = getWord(%line,1);
				}
				%file.close();
				
				//echo("Read playtime from file: " @ %playtimeFile);
				%Playtime = $IDLogger::Playtime[%id]+%playtimeFile; // Add playtime from file to current playtime
				
				//echo("Added file to total: " @ %playtimeFile @ " + " @ $IDLogger::Playtime[%id] @ " = " @ %Playtime);
				//echo("ID=" @ %ID SPC "Name=" @ %Name SPC "Seen=" @ %Seen);
				
				// NAME LOGGING
				if(isFile("config/client/logs/idlog/names/" @ %id @ ".log")) 
				{
					%file.openForRead("config/client/logs/idlog/names/" @ %id @ ".log");
					
					%isNameLine = 0; // IMPORTANT: First line is a date.
					while(!%file.isEOF())
					{
						if(%isNameLine)
						{
							%nameLines++;
							%nameLine[%nameLines] = %file.readLine();
							
							//set isNameLine to 0 for the next line
							%isNameLine = 0;
						}
						else
						{
							//set isNameLine to 1 for the next line
							%isNameLine = 1;
						}
					}
					//%oldName = getSubStr(%nameLine[%namelines],20,99);
					%oldName = %nameLine[%namelines];
					%file.close();
				}
				else
				{
					%newfile = 1;
				} 
				
				//more name logging stuff
				%file.openForAppend("config/client/logs/idlog/names/" @ %id @ ".log");
				%newname = $IDLogger::NameChange[ %i @ "num1" ];
				
				for(%j = 1; %j <= $IDLogger::NameChanges[%i]; %j++)
				{
					if(%oldname $= %newname && %j == 1)
					{
						
						if($IDLogger::FirstNameStopper)
						{
							error("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!LOOP - This needs to be fixed"); //pretty sure i fixed this but i'll leave it for now just in case
							break;
						}
						
						$IDLogger::FirstNameStopper = 1; //fixed: this was set BEFORE checking if($IDLogger:FirstNameStopper)
					}
					else
					{
						%writename = $IDLogger::NameChange[ %i @ "num" @ %j ];
						
						%writedate = $IDLogger::NameChangeDate[ %i @ "num" @ %j ];
						
						$IDLogger::NamesExported++;
						%file.writeLine(%writedate);
						%file.writeLine(%writename);
					}
				}
				$IDLogger::FirstNameStopper = 0;
				
				%file.close();
				
				// end of name logging stuff
				
				%file.openForWrite("config/client/logs/idlog/ids/" @ %ID @ ".log");
				%file.writeLine(%Name);
				%file.writeLine("Last seen: " @ %Seen);
				%file.writeLine("Playtime: " @ %Playtime); 
				%file.close();
			}
			
			// Always echo this
			echo($IDLogger::IDsExported SPC "IDs exported;" SPC $IDLogger::NamesExported SPC "names exported");
			if($IDLogger::ManualIDs)
				echo($IDLogger::ManualIDs SPC "IDs rejected");
			
			%file.delete();
			deleteVariables("$IDLogger::*"); // Once everything is exported, we need to clear it
			deleteVariables("$IDLoggerTime::*");
			
			IDLogger_Init();
		}
		
		Parent::disconnect(%a);
	}
};

activatePackage("IDLogger");
$IDLogger = 1;

function IDLogger_Init()
{
	if($IDLogger::TotalSessionIDs $= "")
		$IDLogger::TotalSessionIDs = 0;
	
	%file = new fileobject();
	$IDLogger::FileCount = getFileCount("config/client/logs/idlog/ids/*");
	
	if(isFile("config/client/logs/idlog/HighestID.log"))
	{
		%file.openForRead("config/client/logs/idlog/HighestID.log");
		$IDLogger::HighestID = %file.readline();
		%versionOld = %file.readLine();
		%file.close();
		
		
		if(%versionOld $= "")
			%versionOld = "1.1.0 or earlier";
		
		// will need to support json eventually
		%file.openForRead("Add-Ons/Client_Log_Players/version.txt");
		%versionNewLine = %file.readLine();
		%file.close();
		%versionNew = getWord(%versionNewLine,1);
		
		%versionOldA = strReplace(%versionOld,"."," ");
		%versionNewA = strReplace(%versionNew,"."," ");
		
		if(getWord(%versionNewA,0) > getWord(%versionOldA,0))
			%newVer = 1;
		else if(getWord(%versionNewA,1) > getWord(%versionOldA,1))
			%newVer = 1;
		else if(getWord(%versionNewA,2) > getWord(%versionOldA,1))
			%newVer = 1;
		
		if(%newVer) // Version-specific fixes
		{
			%echo = "Updated from version " @ %versionOld @ " to " @ %versionNew @ ".";
			
			if(%versionOld $= "1.1.0 or earlier") //1.1.0 fix: blank ".log" files and lan blid files
			{
				fileDelete("config/client/logs/idlog/ids/.log");
				fileDelete("config/client/logs/idlog/names/.log");
				fileDelete("config/client/logs/idlog/ids/999999.log");
				fileDelete("config/client/logs/idlog/names/999999.log");
			}
		}
		
		$IDLogger::VersionExport = %versionNew;
		
		echo("The highest logged ID is" SPC $IDLogger::HighestID @ "!" SPC %echo);
	}
	else
	{
		warn("No highest ID file, setting to 0...");
		$IDLogger::HighestID = 0;
	}
	
	if(isFile("config/client/idlog/HighestID.log")) // Because everything was moved from config/idlog to config/logs/idlog
		error("There are files in the old idlog folder. Please move these to the new folder (config/client/logs/idlog)");
	
	$IDLogger::FileCount = getFileCount("config/client/logs/idlog/ids/*");
	%file.delete();
	
	if(!$Pref::IDLogger::DisableAutoLoad) // Load up the list of names
	{
		statsGui_PL_ClickRefresh();
		StatsGui_PL_SearchStart();
	}
}

function IDLogger_PlaytimeStartID(%id)
{
	if($IDLoggerPref::DisablePlaytime || !isValidBLID(%id))
		return;
	
	if($IDLoggerTime::Start[%id]) // If there's already a start time, we're dealing with multiple clients
	{
		$IDLoggerTime::ClientCount[%id]++; // Keep count of their clients so we know when all of them have disconnected.
		$IDLoggerTime::DuplicateStart[%id,$IDLogger::ClientCount[%id]] = getRealTime()/1000;
		
		if(!$IDLoggerPref::DisableEcho)
			warn("ID Logger - Duplicate client \"" @ $IDLogger::Name[$IDLogger::SessionID[%id]] @ "\" (" @ $IDLoggerTime::ClientCount[%id] @ " total), ignoring...");
	}
	else
	{
		$IDLoggerTime::ClientCount[%id] = 1; // Initialize the client count as 1.
		$IDLoggerTime::Start[%id] = mFloatLength(getRealTime()/1000,0); // Time in seconds
		$IDLoggerTime::ClientCountTotal++; // Client count total does not include duplicates.
		
		//echo("Set start time for " @ %id @ " to " @ $IDLoggerTime::Start[%id] @ ". Total client count:" SPC $IDLoggerTime::ClientCountTotal);
	}
}

function IDLogger_PlaytimeEndID(%id)
{
	if($IDLoggerPref::DisablePlaytime || !isValidBLID(%id))
		return;
	
	if(!$IDLogger::SessionID[%id]) // If there's no session ID, we'll need to re-assign a session ID
	{
		warn("ID Logger - No session ID for " @ %id @ ", a session ID will be re-assigned.");
		$IDLogger::TotalSessionIDs++;
		%SessionID = $IDLogger::TotalSessionIDs;
		$IDLogger::SessionID[%id] = %SessionID;
		
		$IDLogger::ID[%SessionID] = %id;
	}
	
	//echo("ending id " @ %id @ " with a client count of " @ $IDLoggerTime::ClientCount[%id]);
	$IDLoggerTime::ClientCount[%id]--;
	if($IDLoggerTime::ClientCount[%id] > 0)
	{
		if(!$IDLoggerPref::DisableEcho)
			warn("ID Logger - Duplicate client \"" @ $IDLogger::Name[$IDLogger::SessionID[%id]] @ "\" disconnected. User has " @ $IDLoggerTime::ClientCount[%id] @ " clients left.");
		
		return;
	}
	
	$IDLoggerTime::ClientCountTotal--; // Fixed a big mistake. This was decreased before the duplicate client check, causing duplicate clients to decrease the count.
	%realTime = mFloatLength(getRealTime()/1000,0);
	
	if(!$IDLoggerTime::Start[%id])
	{
		error("ID Logger - No start time! Using current time instead.");
		$IDLoggerTime::Start[%id] = %realtime;
	}
	
	//%echoOldPlaytime = $IDLogger::Playtime[%id];
	$IDLogger::Playtime[%id] = $IDLogger::Playtime[%id]+(%realTime - $IDLoggerTime::Start[%id]); // Add playtime to their current session amount
	//echo("realTime - timeStart: " @ %realTime @ " - " @ $IDLoggerTime::Start[%id] @ " = " @ $IDLogger::Playtime[%id]);
	//echo(%id @ " prevPlaytime + newPlaytime: " @ %echoOldPlaytime @ " + " @ %realTime - $IDLoggerTime::Start[%id] @ " = " @ $IDLogger::Playtime[%id]);
	$IDLoggerTime::Start[%id] = 0; // Clear their start time
	
	//echo("Ended ID " @ %id @ ", total playtime is " @ $IDLogger::Playtime[%id]);
	
	return %playtime;
}

function IDLogger_EchoClientCount(%echoAll) // Use this to test if something is wrong
{
	%countDuplicates = 0;
	
	%i = 0;
	%text = NPL_list.getRowText(%i);
	while(%text !$= "")
	{
		%i++;
		%text = NPL_list.getRowText(%i);
		%id = %id = getField(%text,3);
		%count++;
		
		if(%echoAll)
			echo("Client " @ %i @ ": SESSID=" @ $IDLogger::SessionID[%id] @ "; ID=" @ %id @ "; NAME=" @ $IDLogger::Name[$IDLogger::SessionID[%id]]);
		
		if($IDLoggerTime::ClientCount[%id] > 1)
		{
			echo($IDLogger::Name[$IDLogger::SessionID[%id]] @ " has " @ $IDLoggerTime::ClientCount[%id]-1 @ " extra clients.");
			%count = %count+$IDLoggerTime::ClientCount[%id]-1;
			%countDuplicates = %countDuplicates+$IDLoggerTime::ClientCount[%id]-1;
		}
	}
	echo("There are " @ %count @ " total clients; " @ %countDuplicates @ " are duplicate clients.");
}

function IDLogger_CheckClients()
{
	%i = 0;
	%check = NPL_list.getRowText(%i);
	
	while(%check !$= "")
	{
		%i++;
		%check = NPL_list.getRowText(%i);
		
		echo(%check);
	}
}

function isValidBLID(%id)
{
	if(%id == 999999)
		return 0;
	
	if(%id >= 500000) //if ids actually get this high i guess i'll just have to update
	{
		error("isValidBLID - ID " @ %id @ " is above 500k");
		return 0;
	}
	
	if(%id < 0) //if a mysterious broken id is found
	{
		error("isValidBLID - ID " @ %id @ " is < 0");
		return 0;
	}
	
	return 1;
}

//IDLogger_LogID(Name, BLID, Date)
//If a date is specified, the ID is treated as a "Manual Entry" and will be ignored if it already exists.
//Playtime also won't be logged if a date is specified.
function IDLogger_LogID(%name,%id,%date)
{
	if(!$IDLoggerPref::DisableEcho)
		echo("ID Logger - Registering name \"" @ %name @ "\" to id " @ %id);
	
	if(%date && isFile("config/client/logs/idlog/ids/" @ %id @ ".log"))
	{
		warn("ID Logger - Manual entry ID already exists, ignoring...");
		$IDLogger::ManualIDs++;
		return;
	}
	
	if(!isValidBLID(%id))
		return;
	
	if(!$IDLogger::SessionID[%id])
	{
		//echo("This is a new session ID!");
		
		$IDLogger::TotalSessionIDs++;
		%SessionID = $IDLogger::TotalSessionIDs;
		$IDLogger::SessionID[%id] = %SessionID;
		
		// NAME COUNT
		$IDLogger::NameChanges[%SessionID]++;
		
		// NAME
		$IDLogger::NameChange[ %SessionID @ "num" @ $IDLogger::NameChanges[%SessionID] ] = %name; //$IDLogger::NameChange[SessID]num[Count]
		
		// NAME DATE
		$IDLogger::NameChangeDate[ %SessionID @ "num" @ $IDLogger::NameChanges[%SessionID] ] = getDateTime(); //$IDLogger::NameChangeDate[SessID]num[Count]
	}
	else
	{
		//echo("This is an existing session ID!");
		%SessionID = $IDLogger::SessionID[%id]; 
		
		//name logging stuff
		%oldname = $IDLogger::NameChange[ %SessionID @ "num" @ $IDLogger::NameChanges[%SessionID] ];
		//echo("Old name: " @ %oldname);
		if(%name !$= %oldname)
		{
			//echo("New name " @ $IDLogger::NameChanges[%SessionID]+1 @ "! (" @ %oldname @ " !$= " @ %name @ ")");
			$IDLogger::NameChanges[%SessionID]++;
			
			// NAME
			$IDLogger::NameChange[ %SessionID @ "num" @ $IDLogger::NameChanges[%SessionID] ] = %name; //$IDLogger::NameChange[SessID]num[Count]
			
			// NAME DATE
			$IDLogger::NameChangeDate[ %SessionID @ "num" @ $IDLogger::NameChanges[%SessionID] ] = getDateTime(); //$IDLogger::NameChangeDate[SessID]num[Count]
			//echo("Setting $IDLogger::NameChangeDate[" @ %sessionID @ "]num[" @ $IDLogger::NameChanges[%sessionID] @ "] to " @ getDateTime());
		}
	}
	
	if(%id > $IDLogger::HighestID)
	{
		if(!$IDLoggerPref::DisableEcho)
			echo("Highest ID updated! (" @ %id @ " > " @ $IDLogger::HighestID @ ")");
		$IDLogger::HighestID = %id;
	}
	
	$IDLogger::Name[%SessionID] = %name;
	$IDLogger::ID[%SessionID] = %id;
	
	if(!%date)
	{
		$IDLogger::Seen[%SessionID] = getDateTime();
	}
	else
	{
		//echo("Using manual date for this ID (" @ %date @ ")");
		$IDLogger::Seen[%SessionID] = %date;
		//set this up with playtime logging too
	}
	
	$IDLogger::GetBLID[%SessionID] = %id;
	$IDLogger::GetNameByBLID[%id] = %name;
}

// This function will return the total number of name changes logged. Note: Session IDs are not counted.
// echo("Average: " @ IDLogger_getTotalNameChanges()/getFileCount("config/client/logs/idlog/names/*"));
function IDLogger_getTotalNameChanges()
{
	%file = new fileobject();
	%count = getFileCount("config/client/logs/idlog/names/*");
	
	for(%i = 1; %i <= %count; %i++)
	{
		if(%i == 1)
			%currentFile = findFirstFile("config/client/logs/idlog/names/*");
		else
			%currentFile = findNextFile("config/client/logs/idlog/names/*");
		
		%nameLines = %nameLines+IDLogger_getNameChanges(%currentFile,%file);
	}
	
	%file.delete();
	return %nameLines;
}

//This function will return the number of name changes in a file.
function IDLogger_getNameChanges(%currentFile,%fileObject)
{
	if(!isFile(%currentFile))
		return -1;
	
	if(%fileObject $= "")
		%file = new fileObject();
	else
		%file = %fileObject;
	
	%file.openForRead(%currentFile);
	
	%isNameLine = 0; // IMPORTANT: First line is a date.
	while(!%file.isEOF())
	{
		%file.readLine();
		if(%isNameLine)
		{
			%nameLines++;
			
			//set isNameLine to 0 for the next line
			%isNameLine = 0;
		}
		else
		{
			//set isNameLine to 1 for the next line
			%isNameLine = 1;
		}
	}
	
	%file.close();
	if(!%fileObject)
		%file.delete();
	return %nameLines;
}

// This will delete ALL playtime data from ID files. There will be a prompt for this eventually since existing playtime data is screwed up.
function IDLogger_DeleteAllPlaytimeData()
{
	%count = getFileCount("config/client/logs/idlog/ids/*");
	%file = new fileobject();
	
	for(%i = 1; %i <= %count; %i++)
	{
		if(%i == 1)
			%currentFile = findFirstFile("config/client/logs/idlog/ids/*");
		else
			%currentFile = findNextFile("config/client/logs/idlog/ids/*");
		
		%file.openForRead(%currentFile);
		%name = %file.readLine();
		%seen = %file.readLine();
		%time = %file.readLine();
		%file.close();
		
		if(%time !$= "") // No need to rewrite if the time is already blank
		{
			%filesWritten++;
			
			%file.openForWrite(%currentFile);
			%file.writeLine(%name);
			%file.writeLine(%seen);
			%file.close();
		}
		else
			%filesSkipped++;
		
		//echo("Cleared " @ %currentFile @ " playtime: " @ %time);
	}
	echo("FINISHED - Cleared playtime from " @ %filesWritten @ " files.");
	%file.delete();
}

// This function will return the session IDs with a name that matches or contains the text in %name. (Note: %name is not case-sensitive)
// Set %exact to 1 if you only want names that are an exact match.
// %returnCount is the number of IDs you want to return (0 to return all of them)
function IDLogger_FindSessionIDByName(%name,%exact,%returnCount)
{
	%name = strupr(%name);
	for(%i = 1; %i <= $IDLogger::TotalSessionIDs; %i++)
	{
		%returnThis = 0;
		if(%exact)
		{
			if(strupr($IDLogger::Name[%i]) $= %name)
				%returnThis = 1;
		}
		else
		{
			if(strstr(strupr($IDLogger::Name[%i]),%name) >= 0)
				%returnThis = 1;
		}
		
		if(%returnThis)
		{
			if(%return $= "") // Avoid a blank space at the beginning
				%return = %i;
			else
				%return = %return SPC %i;
		}
			
		if(%returnCount > 0 && getWordCount(%return) >= %returnCount)
			break;
	}
	return %return;
}

//// TO DO: name history searching. (use the ids folder for the loop in case name history files are missing)
// This function will search the log files for %name. Arguments are the same as IDLogger_FindSessionIDByName.
// When %repeat is set to 1, %callback will be called each time a match is found instead of when the search is complete. The result will only contain the last found ID.
// %returnCount is how many IDs you want to return. Setting it to 0 returns all of them. (WARNING: Returning more than 255 may cause the game to close.)
// Callback syntax: Quotation marks are included, don't add your own. Format: %inc = increment; %blid = BLID; %name = name; %seen = seen
// The result is "returned" with %callback. Example: "echo(\"Found player named %n with ID %id, last seen on %s.\");"
// This cannot be used to search name history logs yet.
// WARNING: The game may close if this function returns 255 or more IDs at once with %repeat off.
function IDLogger_SearchFiles(%name,%exact,%returnCount,%callback,%repeat,%delay,%failCallback)
{
	if(%delay < 0)
		%delay = 0;
	
	cancel($IDLogger::SearchLoop);
	%file = new fileobject();

	$IDLogger::SearchLoop = schedule(%delay,0,IDLogger_FileSearchLoop,%i,%name,%exact,%returnCount,%callback,%failCallback,%repeat,%file,%firstFile,%return,%delay);
}

function IDLogger_FileSearchLoop(%i,%name,%exact,%returnCount,%callback,%failCallback,%repeat,%file,%firstFile,%return,%delay,%idCount)
{
	%folder = "ids";
	
	%i++;
	if(%i == 1)
	{
		%currentFile = findFirstFile("config/client/logs/idlog/" @ %folder @ "/*");
		%firstFile = %currentFile;
	}
	else
		%currentFile = findNextFile("config/client/logs/idlog/" @ %folder @ "/*");
	
	%blidFile = strreplace(%currentFile,"config/client/logs/idlog/" @ %folder @ "/","");
	%blidFile = strreplace(%blidFile,".log","");
	
	%file.openForRead(%currentFile);
	%nameFile = %file.readLine();
	%seenFile = strReplace(%file.readLine(),"Last seen: ","");
	%file.close();
	
	%returnThis = 0;
	if(%exact)
	{
		if(strUpr(%nameFile) $= strUpr(%name))
			%returnThis = 1;
	}
	else
	{
		if(strstr(strUpr(%nameFile),strUpr(%name)) >= 0)
			%returnThis = 1;
	}
	
	if(%returnThis && %callback !$= "") // We are "returning" the information
	{
		if(%repeat) // Repeat means we only need the current ID.
		{
			%return = %blidFile;
			%returnName = %nameFile;
			%returnSeen = %seenFile;
		}
		else
		{
			if(%return $= "") // Avoid a blank space at the beginning
			{
				%return = %blidFile;
				%returnName = %nameFile;
				%returnSeen = %seenFile;
			}
			else
			{
				%return = %return SPC %blidFile; // Warning: The game may close when the word count reaches 255
				%returnName = %returnName SPC %nameFile;
				%returnSeen = %returnSeen SPC %seenFile;
			}
		}
		
		if(%i > $IDLogger::FileCount || %currentFile $= %firstFile && %i != 1) // Reached the last file.
		{
			%file.delete();
			return %return;
		}
		
		// Might add a way to disable these if it speeds things up.
		%str = strreplace(%callback,"%blid","\"" @ %return @ "\"");
		%str = strreplace(%str,"%inc","\"" @ %i @ "\"");
		%str = strreplace(%str,"%name","\"" @ %returnName @ "\"");
		%str = strreplace(%str,"%seen","\"" @ %returnSeen @ "\"");
		
		if(%repeat)
		{
			eval(%str);
			%evalDone = 1;
			%idCount++;
		}
		
		if(%returnCount > 0 && %idCount >= %returnCount)
		{
			if(!%evalDone && %str !$= "")
			{
				eval(%str);
				%evalDone = 1;
				%idCount++;
			}
			
			%file.delete();
			return %return;
		}
	}
	else if(%failCallback !$= "") // If a callback is defined for when nothing is found.
	{
		%str = strreplace(%failCallback,"%inc",%i);
		eval(%str);
		%evalDone = 1;
	}
	
	$IDLogger::SearchLoop = schedule(%delay,0,IDLogger_FileSearchLoop,%i,%name,%exact,%returnCount,%callback,%failCallback,%repeat,%file,%firstFile,%return,%delay,%idCount);
}

// Fix for missing name history files.
function IDLogger_FixNameHistoryFiles()
{
	%file = new fileObject();
	%count = getFileCount("config/client/logs/idlog/ids/*");
	
	for(%i = 1; %i <= %count; %i++)
	{
		if(%i == 1)
			%currentFile = findFirstFile("config/client/logs/idlog/ids/*");
		else
			%currentFile = findNextFile("config/client/logs/idlog/ids/*");
		
		%blidFile = strreplace(%currentFile,"config/client/logs/idlog/ids/","");
		
		if(!isFile("config/client/logs/idlog/names/" @ %blidFile))
		{
			// Code to create name log here
			// This might be a bad idea. It would probably be better to only create a name log when necessary. (The only problem is that this would require changes to the original code)
		}
	}
	
	%file.delete();
}

IDLogger_Init();
/////Console Functions/////
function IDLogger_ViewStats(%id)
{
	%filePathA = "config/client/logs/idlog/ids/" @ %id @ ".log";
	if(!isFile(%filePathA))
	{
		warn("IDLogger_ViewStats - Could not find the specified ID.");
		return;
	}
	
	%file = new fileObject();
	%file.openForRead(%filePathA);
	%echoName = "Name: " @ %file.readLine();
	%echoSeen = %file.readLine();
	%playtime = getWord(%file.readLine(),1);
	%file.close();
	
	if(%playtime)
		%echoPlaytime = "" NL "Playtime: " @ mFloatLength(%playtime/60/60,1) @ " hours";
	else
		%echoPlaytime = "" NL "Playtime: None";
	
	// Name history check
	if(isFile("config/client/logs/idlog/names/" @ %id @ ".log"))
		%echoNameChanges = "" NL "Name changes: " @ IDLogger_getNameChanges("config/client/logs/idlog/names/" @ %id @ ".log")-1;
	else
		%echoNameChanges = "" NL "Name changes: 0";
	
	echo(%echoName NL %echoSeen @ %echoPlaytime @ %echoNameChanges);
}

function IDLogger_ViewNameHistory(%id)
{
	%filePath = "config/client/logs/idlog/names/" @ %id @ ".log";
	if(!isFile(%filePath))
	{
		warn("IDLogger_ViewNameHistory - Could not find the specified ID.");
		return;
	}
	
	%file = new fileObject();
	%file.openForRead(%filePath);
	
	%isNameLine = 0; // IMPORTANT: First line is a date.
	while(!%file.isEOF())
	{
		if(%isNameLine)
		{
			echo("[" @ %dateLine @ "]" SPC %file.readLine());
			
			//set isNameLine to 0 for the next line
			%isNameLine = 0;
		}
		else
		{
			//set isNameLine to 1 for the next line
			%dateLine = %file.readLine();
			%isNameLine = 1;
		}
	}
	
	%file.close();
	%file.delete();
}

//Simplified version of the IDLogger_SearchFiles function. Set %clipboard to 1 to copy the result to your clipboard.
function IDLogger_Search(%name,%clipboard,%callback)
{
	echo("Searching for name...");
	
	if(!%callback)
		%callback = "echo(\"IDLogger_Search - " @ %name @ " - %1\");";
	
	if(%clipboard)
		%callback = "setClipboard(\"%1\");" @ %callback;
	
	IDLogger_SearchFiles(%name,0,250,%callback,0,0);
}
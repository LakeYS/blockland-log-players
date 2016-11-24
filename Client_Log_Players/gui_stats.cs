exec("./statsGui.gui");

if(!$statsGuiKeybind) //rename?
{
	$statsGuiKeybind = 1;
	$remapDivision[$remapCount] = "Statistics GUI";
	$remapName[$remapCount] = "Open";
	$remapCmd[$remapCount] = "statsGui_Open";
	$remapCount++;
}

function statsGui_Open()
{
	canvas.pushDialog(statsGui);
}

function statsGui::SetPane(%gui,%pane)
{
	%pane = "statsPane_" @ %pane;
	
	for(%i = 0; %i <= statsGui_window.getCount()-1; %i++)
	{
		%obj = statsGui_window.getObject(%i);
		if(%obj.getName() $= %pane)
			%obj.setVisible(1);
		else if(%obj.getClassName() $= GuiControl)
		{
			if(!isObject(%pane)) // If no pane is specified, default to the first one
			{
				%obj.setVisible(1);
				break;
			}
			else
				%obj.setVisible(0);
		}
	}
}

function statsGui::Start(%gui) // Default actions for onWake
{
	
}
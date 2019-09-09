float StepLenght = 1; //Meters
int _LogLinesCount = 4;
string IntKey = "!KPC";
string MainKey = "[KPC]";

struct PrinterOBj
{
    public int State;
    public IMyMotorAdvancedStator Motor;
    public List<IMyPistonBase> Pistons;
    public List<IMyShipWelder> Welders;
    public List<IMyTextPanel> TextPanels;
    public float MotorTarget;
    public float PistonStep;
    public float PistonEx;
    public int PistonsDirection;
    public float PistonHighest;
    public float PistonLowest;
    public List<string> LogLines;
}

PrinterOBj Core = new PrinterOBj
{
    State = 0,
    Pistons = new List<IMyPistonBase>(),
    Welders = new List<IMyShipWelder>(),
    TextPanels = new List<IMyTextPanel>(),
    LogLines = new List<string>(),
    MotorTarget = 1000
};

string[] defaultData = {
    "Kivi's Printer Controller Group Settings:",
    "=========================================",
    "GroupName=#default",
    "",
};
string[] PistonDefaultData = {
    "Basic Settings:",
    "===============",
    "Set the X/Y/Z Object Axis below",
    "XYZMapping=",
    "",
    "Piston Settings:",
    "================",
    "min=0",
    "max=10"
};

public Program()
{
    if (Me.CustomName.ToLower().Contains(IntKey.ToLower()) == true) {
        Me.CustomName = Me.CustomName.Replace(IntKey,MainKey);
    }
    else if (Me.CustomName.ToLower().Contains(MainKey.ToLower()) == true) {
        Me.CustomName = Me.CustomName.Replace(MainKey,MainKey);
    }
    else {
        Me.CustomName = Me.CustomName + " " + MainKey;
    }
    CheckCD(Me);
    List<IMyPistonBase> t_Pistons = new List<IMyPistonBase>();
    GridTerminalSystem.GetBlocksOfType(t_Pistons, R => R.CustomName.ToLower().Contains(IntKey.ToLower()));
    foreach (IMyPistonBase item in t_Pistons) {
        item.CustomName = item.CustomName.Replace(IntKey,MainKey);
        //var customData = item.CustomData.Split('\n');
        string p_newCustomData = Me.CustomData + "\n\n";
            foreach (var line in PistonDefaultData) {
                string t_line = line;
                if (line == "min=0") {
                    t_line = "min=" + item.MinLimit;
                }
                if (line == "max=10") {
                    t_line = "max=" + item.MaxLimit;
                }
                p_newCustomData += t_line + "\n";
            }
            item.CustomData = p_newCustomData.TrimEnd('\n');
    }
    List<IMyTextPanel> t_TextPanel = new List<IMyTextPanel>();
    GridTerminalSystem.GetBlocksOfType(t_TextPanel, R => R.CustomName.ToLower().Contains(IntKey.ToLower()));
    foreach (IMyTextPanel item in t_TextPanel) {
        item.CustomName = item.CustomName.Replace(IntKey,MainKey);
        item.CustomData = Me.CustomData;
    }
    List<IMyMotorAdvancedStator> t_Motors = new List<IMyMotorAdvancedStator>();
    GridTerminalSystem.GetBlocksOfType(t_Motors, R => R.CustomName.ToLower().Contains(IntKey.ToLower()));
    foreach (IMyMotorAdvancedStator item in t_Motors) { 
        item.CustomName = item.CustomName.Replace(IntKey,MainKey);
        item.CustomData = Me.CustomData;
    }

    string MyGroupName = ReadCDString(Me, "GroupName");
    GridTerminalSystem.GetBlocksOfType(Core.Pistons, R => R.CustomName.ToLower().Contains(MainKey.ToLower()) && ReadCDString(R, "GroupName").ToLower().Contains(MyGroupName.ToLower()));
    GridTerminalSystem.GetBlocksOfType(Core.TextPanels, R => R.CustomName.ToLower().Contains(MainKey.ToLower()) && ReadCDString(R, "GroupName").ToLower().Contains(MyGroupName.ToLower()));
    if (Core.Pistons.Count > 0)
    {
        Core.State = 1;
        Core.PistonStep = StepLenght / Core.Pistons.Count;
        Core.PistonsDirection = Math.Sign(Core.Pistons[0].Velocity);
        Core.PistonEx = Core.Pistons[0].MaxLimit;
        Core.PistonLowest = Core.Pistons[0].LowestPosition;
        Core.PistonHighest = Core.Pistons[0].HighestPosition;
    }
    List<IMyMotorAdvancedStator> M = new List<IMyMotorAdvancedStator>();
    GridTerminalSystem.GetBlocksOfType(M, R => R.CustomName.ToLower().Contains(MainKey.ToLower()) && ReadCDString(R, "GroupName").ToLower().Contains(MyGroupName.ToLower()));
    if (M.Count != 1) { WriteToScreen("Check Up the Motor"); } else { Core.Motor = M[0]; }
    Core.State = M.Count != 1 ? 0 : Core.State;
    WriteToScreen("Pistons Count : " + Core.Pistons.Count);
    WriteToScreen("Motor : " + (M.Count == 1 ? "Ok" : "Error"));
    Runtime.UpdateFrequency = Core.State == 0 ? UpdateFrequency.None : UpdateFrequency.Update10;
}

public void Main(string Argument, UpdateType UpdateSource)
{
    if (!String.IsNullOrEmpty(Argument) && Core.State == 1) { ExecuteArgument(Argument); }
    else { Thread(); WriteToScreen(); }
}
void ExecuteArgument(string Argument)
{
    string A = Argument.ToLower();
    if (A == "setangle") { A = "setangle=0"; }
    if (A.StartsWith("setangle=")) { Core.MotorTarget = (float)Recognize(Argument); WriteToScreen("Manual Angle = " + Core.MotorTarget); }
    if (A.StartsWith("setvelocity=")) { float V = (float)Recognize(Argument); WriteToScreen("Manual Velocity = " + V); Core.Motor.TargetVelocityRPM = V; Core.MotorTarget = 1000; }
    if (A == "extend")
    {
        Core.PistonEx += Core.PistonStep; if (Core.PistonEx > Core.PistonHighest) { Core.PistonEx = Core.PistonHighest; }
        Core.PistonsDirection = 1;
        SetPistons();
    }
    if (A == "retract")
    {
        Core.PistonEx -= Core.PistonStep; if (Core.PistonEx < Core.PistonLowest) { Core.PistonEx = Core.PistonLowest; }
        Core.PistonsDirection = -1;
        SetPistons();
    }
    if (A.StartsWith("setextenion="))
    {
        float V = (float)Recognize(Argument) / Core.Pistons.Count; Core.PistonsDirection = Math.Sign(V - Core.PistonEx); Core.PistonEx = V;
        SetPistons();
    }
}

void SetPistons()
{
    foreach (IMyPistonBase P in Core.Pistons)
    { P.MaxLimit = Core.PistonEx; P.MinLimit = Core.PistonEx - Core.PistonStep; P.Velocity = .1f * Math.Sign(Core.PistonsDirection); }
}

void Thread()
{
    if (Core.MotorTarget != 1000) { if (SetMotorAngle(Core.MotorTarget, ref Core.Motor)) { Core.MotorTarget = 1000; }; }
}

float AR = (float)Math.PI / 180;
bool SetMotorAngle(float Angle, ref IMyMotorAdvancedStator Motor)
{
    float BrakeAngle = 3;
    float DX = Angle - Motor.Angle / AR;
    float Diff = 180 - (DX + 360) % 360;
    DX = Math.Abs(DX);
    bool R = DX < .1f;
    Motor.TargetVelocityRPM = (DX > BrakeAngle ? 1 : R ? 0 : .1f) * Math.Sign(Diff);
    return R;
}

double Recognize(string Argument, double Default = 0, string Separator = "=")
{
    string[] C = Argument.Split(new string[] { Separator }, StringSplitOptions.None);
    return C.Length > 1 ? Convert.ToDouble(C[1]) : Default;
}

string LastText = ""; string xLog = "";
void WriteToScreen (string Argument = null){

    if (!String.IsNullOrEmpty(Argument))
    {
        Core.LogLines.Insert(0, " " + sNow() + " " + Argument);
        if (Core.LogLines.Count > _LogLinesCount) { Core.LogLines.RemoveAt(_LogLinesCount); }
        xLog = String.Join("\n", Core.LogLines);
    }
    string Out = " Kivi's Printer Controller is " + (Core.State == 0 ? " Not Active" : "Activate") + "\n\n"
        + " Piston Extension = " + (Core.Pistons.Count * Core.PistonEx).ToString("0.0") + "\n"
        + xLog;
    if (Out != LastText)
    {
        LastText = Out;
        if (Core.TextPanels.Count > 0) {
            foreach (IMyTextPanel T in Core.TextPanels) {
                T.ContentType=VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
                T.WriteText(Out);
                Echo(Out);
            }
        }
        else {
            Echo(Out);
        }
    }
}
string sNow() => DateTime.Now.ToString("HH:mm:ss");

/// <summary>
		/// Read an object's custom data
		/// </summary>
		/// <param name="t_object">t_object to read</param>
		/// <param name="field">Field to read</param>
		/// <returns>Fieldvalue as bool</returns>
string ReadCDString(IMyTerminalBlock t_object, string field)
{
	CheckCD(t_object);
	var customData = t_object.CustomData.Split('\n');

	foreach (var line in customData) {
		if (line.Contains(field + "=")) {
			return line.Replace(field + "=", "");
		}
	}
	return "";
}

/// <summary>
		/// Checks a t_object's custom data and restores the default custom data, if it is too short
		/// </summary>
		/// <param name="t_object">t_object to check</param>
void CheckCD(IMyTerminalBlock t_object)
{
	var customData = t_object.CustomData.Split('\n');

	// Create new default customData if a too short one is found and set the default font size 
	if (string.IsNullOrWhiteSpace(Me.CustomData)) { //7 is the number of char in "default" (customData.Length <= (defaultData.Length - 7))
		string newCustomData = "";

		foreach (var item in defaultData) {
			newCustomData += item + "\n";
		}

		t_object.CustomData = newCustomData.TrimEnd('\n');
	}
}
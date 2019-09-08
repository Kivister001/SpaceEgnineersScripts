float StepLenght = 1; //Meters
int _LogLinesCount = 4;
string _Key = "_3D";

struct tCore
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

tCore Core = new tCore
{
    State = 0,
    Pistons = new List<IMyPistonBase>(),
    Welders = new List<IMyShipWelder>(),
    TextPanels = new List<IMyTextPanel>(),
    LogLines = new List<string>(),
    MotorTarget = 1000
};

public Program()
{
    Me.CustomName = " •• 3D Builder";
    GridTerminalSystem.GetBlocksOfType(Core.TextPanels, R => R.CustomName.ToLower().Contains(_Key.ToLower()));
    GridTerminalSystem.GetBlocksOfType(Core.Pistons, R => R.CustomName.ToLower().Contains(_Key.ToLower()));
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
    GridTerminalSystem.GetBlocksOfType(M, R => R.CustomName.ToLower().Contains(_Key.ToLower()));
    if (M.Count != 1) { ToScreen("Check Up the Motor"); } else { Core.Motor = M[0]; }
    Core.State = M.Count != 1 ? 0 : Core.State;
    ToScreen("Pistons Count : " + Core.Pistons.Count);
    ToScreen("Motor : " + (M.Count == 1 ? "Ok" : "Error"));
    Runtime.UpdateFrequency = Core.State == 0 ? UpdateFrequency.None : UpdateFrequency.Update10;
}

public void Main(string Argument, UpdateType UpdateSource)
{
    if (!String.IsNullOrEmpty(Argument) && Core.State == 1) { ProceedArgument(Argument); }
    else { Thread(); ToScreen(); }
}

void ProceedArgument(string Argument)
{
    string A = Argument.ToLower();
    if (A == "m") { A = "m=0"; }
    if (A.StartsWith("m=")) { Core.MotorTarget = (float)Recognize(Argument); ToScreen("Manual Angle = " + Core.MotorTarget); }
    if (A.StartsWith("v=")) { float V = (float)Recognize(Argument); ToScreen("Manual Velocity = " + V); Core.Motor.TargetVelocityRPM = V; Core.MotorTarget = 1000; }
    if (A == "e")
    {
        Core.PistonEx += Core.PistonStep; if (Core.PistonEx > Core.PistonHighest) { Core.PistonEx = Core.PistonHighest; }
        Core.PistonsDirection = 1;
        SetPistons();
    }
    if (A == "r")
    {
        Core.PistonEx -= Core.PistonStep; if (Core.PistonEx < Core.PistonLowest) { Core.PistonEx = Core.PistonLowest; }
        Core.PistonsDirection = -1;
        SetPistons();
    }
    if (A.StartsWith("e="))
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
void ToScreen(string Argument = null)
{
    if (!String.IsNullOrEmpty(Argument))
    {
        Core.LogLines.Insert(0, " " + sNow() + " " + Argument);
        if (Core.LogLines.Count > _LogLinesCount) { Core.LogLines.RemoveAt(_LogLinesCount); }
        xLog = String.Join("\n", Core.LogLines);
    }
    string Out = " 3D Builder." + Me.CubeGrid.CustomName + "  is " + (Core.State == 0 ? " Not Active" : "Activate") + "\n\n"
        + " Piston Extraction = " + (Core.Pistons.Count * Core.PistonEx).ToString("0.0") + "\n"
        + xLog;
    if (Out != LastText)
    { 
        LastText = Out; 
        if (Core.TextPanels.Count > 0) { 
            foreach (IMyTextPanel T in Core.TextPanels) { 
                T.ContentType=VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE; 
                T.WriteText(Out); 
            } 
        } 
        else { 
            Echo(Out); 
        } 
    }
}

string sNow() => DateTime.Now.ToString("HH:mm:ss");
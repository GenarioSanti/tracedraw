using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Diagnostics;
using System.Security.Principal;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Threading;

static class App {
 public static string Home=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"SSPrint","BannerCutTraceDraw");
 public static string Mailbox {get{return Path.Combine(Home,"pedidos");}}
 public static string Exe {get{return Application.ExecutablePath;}}
 public static void Error(Exception e){Log(e.ToString());MessageBox.Show(e.Message,"BannerCut - TraceDraw",MessageBoxButtons.OK,MessageBoxIcon.Exclamation);}
 public static void Log(string s){try{Directory.CreateDirectory(Home);File.AppendAllText(Path.Combine(Home,"integracao.log"),DateTime.Now.ToString("s")+" "+s+Environment.NewLine);}catch{}}
 public static string Camera(){string f=Path.Combine(Home,"camera-path.txt");if(File.Exists(f)){string s=File.ReadAllText(f).Trim();if(File.Exists(s))return s;}return @"C:\Program Files\CameraCut\CameraCut.exe";}
 public static bool Elevated(){using(var id=WindowsIdentity.GetCurrent())return new WindowsPrincipal(id).IsInRole(WindowsBuiltInRole.Administrator);}
 public static void Elevate(string argument){Process.Start(new ProcessStartInfo(Exe,argument){UseShellExecute=true,Verb="runas",WorkingDirectory=Path.GetDirectoryName(Exe)});}
 public static void Config(){
  if(!Elevated()){Session("config");return;}
  string camera=Camera();if(!File.Exists(camera)){using(var d=new OpenFileDialog{Title="Localizar CameraCut.exe",Filter="CameraCut.exe|CameraCut.exe"}){if(d.ShowDialog()!=DialogResult.OK)return;camera=d.FileName;Directory.CreateDirectory(Home);File.WriteAllText(Path.Combine(Home,"camera-path.txt"),camera);}}
  Log("Abrindo configuracoes: " + camera); using(var dialog=new CutSettings(camera)){dialog.StartPosition=FormStartPosition.CenterScreen;dialog.ShowInTaskbar=true;dialog.TopMost=true;dialog.ShowDialog();} Log("Configuracoes fechadas.");
 }
 [STAThread] public static int Main(string[] args){
  Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
  try{
   if(args.Length==2&&(args[0]=="--session-config"||args[0]=="--session-send")){
    if(!Elevated()||WindowsIdentity.GetCurrent().User.Value!=args[1])throw new Exception("Autorize com a mesma conta do Windows usada no TraceDraw.");
    bool created;using(var session=new Mutex(true,SessionName(),out created)){
     if(!created)return 0;
     var broker=new Broker();broker.BeginInitial(()=>{if(args[0]=="--session-config")Config();else CameraLink.Send();});Application.Run(broker);
    }return 0;
   }
   if(args.Length==2&&args[0]=="--install-bundled"){Install.Bundled(args[1]);return 0;}
   if(args.Length>=3&&args[0]=="--convert"){var job=Geometry.ReadSvg(args[1]);File.WriteAllText(args[2],job.Text,Encoding.ASCII);return 0;}
   if(args.Length>=3&&args[0]=="--gms"){Gms.Write(args[2],File.ReadAllText(args[1]));return 0;}
   if(args.Length>=2&&args[0]=="--selftest"){Tests.Run(args[1]);return 0;}
   if(args.Length>0&&args[0]=="--config"){Config();return 0;}
   if(args.Length>0&&args[0]=="--deliver"){CameraLink.Send();return 0;}
   if(args.Length>0&&(args[0]=="--watch"||args[0]=="--start")){
    bool created;using(var mutex=new Mutex(true,"Local\\SSPrintBannerCut_"+WindowsIdentity.GetCurrent().User.Value,out created)){
     if(args[0]=="--start")OpenTraceDraw();
     if(created)Application.Run(new Broker());
    }return 0;
   }
   if(args.Length>0&&args[0]=="--uninstall"){Install.Uninstall();return 0;}
   Application.Run(new Setup());return 0;
  }catch(Exception ex){if(args.Length>0&&(args[0]=="--selftest"||args[0]=="--convert"||args[0]=="--gms")){File.WriteAllText(Path.Combine(Path.GetDirectoryName(Exe),"erro-teste.txt"),ex.ToString());return 2;}Error(ex);return 1;}
 }
 static void OpenTraceDraw(){
  if(Process.GetProcessesByName("tracedraw").Length>0)return;
  string f=Path.Combine(Home,"tracedraw-path.txt");if(File.Exists(f)){string p=File.ReadAllText(f).Trim();if(File.Exists(p)){Process.Start(p);return;}}
  MessageBox.Show("A integracao esta ativa. Abra o TraceDraw pelo atalho que voce usa normalmente.","BannerCut");
 }
 public static string SessionName(){return "Local\\SSPrintBannerCutAuthorized_"+WindowsIdentity.GetCurrent().User.Value;}
 public static bool FreshSession(DateTime stamp,DateTime now){return stamp<=now&&now-stamp<TimeSpan.FromSeconds(30);}
 public static bool SessionActive(){try{return FreshSession(File.GetLastWriteTimeUtc(Path.Combine(Home,"sessao-ativa.txt")),DateTime.UtcNow);}catch(IOException){return false;}catch(UnauthorizedAccessException){return false;}}
 public static void Session(string action){
  if(action!="config"&&action!="send")throw new ArgumentException("Acao invalida.");
  if(SessionActive())throw new Exception("A sessao autorizada ja esta ativa. Use novamente o botao no TraceDraw.");
  Elevate("--session-"+action+" "+WindowsIdentity.GetCurrent().User.Value);
 }
}
class Broker:ApplicationContext {
 NotifyIcon icon;System.Windows.Forms.Timer timer;DateTime send,config,stop;bool busy;
 public void BeginInitial(Action action){busy=true;var first=new System.Windows.Forms.Timer{Interval=800};first.Tick+=(s,e)=>{first.Stop();first.Dispose();try{action();}catch(Exception ex){Report(ex);}finally{busy=false;}};first.Start();}
 void Report(Exception ex){App.Log(ex.ToString());string message=ex is System.ComponentModel.Win32Exception&&((System.ComponentModel.Win32Exception)ex).NativeErrorCode==1223?"Autorizacao cancelada. A integracao continua ativa. Clique novamente em Enviar ou Configurar quando quiser tentar.":ex.Message;icon.Visible=true;icon.ShowBalloonTip(8000,"BannerCut",message,ToolTipIcon.Warning);}
 void Pulse(){if(App.Elevated())try{File.WriteAllText(Path.Combine(App.Home,"sessao-ativa.txt"),Process.GetCurrentProcess().Id.ToString());}catch(Exception ex){App.Log("Falha no sinal de sessao: "+ex.Message);}}
 public Broker(){
  Directory.CreateDirectory(App.Mailbox);send=Stamp("enviar.svg");config=Stamp("configurar.svg");stop=Stamp("parar.txt");
  var menu=new ContextMenuStrip();menu.Items.Add("Configuracoes",null,(s,e)=>{if(busy)return;busy=true;try{App.Config();}catch(Exception ex){Report(ex);}finally{busy=false;}});menu.Items.Add("Instrucoes",null,(s,e)=>Process.Start(Path.Combine(Path.GetDirectoryName(App.Exe),"LEIA-ME.txt")));menu.Items.Add("Encerrar integracao",null,(s,e)=>{File.WriteAllText(Path.Combine(App.Mailbox,"parar.txt"),DateTime.UtcNow.ToString("O"));ExitThread();});
  icon=new NotifyIcon{Icon=SystemIcons.Application,Text="BannerCut para TraceDraw - ativo",Visible=true,ContextMenuStrip=menu};
  menu.Items.Insert(0,new ToolStripMenuItem("Ver estado",null,(s,e)=>MessageBox.Show("Integracao RC7 ativa.\r\nSessao autorizada: "+(App.Elevated()||App.SessionActive()?"sim":"aguarda a primeira autorizacao")+"\r\nDepois de reiniciar a integracao, clique novamente em Enviar.\r\nFalhas ficam em Abrir registro.","BannerCut")));
  menu.Items.Insert(1,new ToolStripMenuItem("Abrir registro",null,(s,e)=>{try{Process.Start(new ProcessStartInfo("notepad.exe","\""+Path.Combine(App.Home,"integracao.log")+"\""){UseShellExecute=true});}catch(Exception ex){Report(ex);}}));
  Pulse();timer=new System.Windows.Forms.Timer{Interval=500};timer.Tick+=Tick;timer.Start();App.Log("RC7 iniciado. PID="+Process.GetCurrentProcess().Id+" autorizado="+App.Elevated());
 }
 DateTime Stamp(string f){return File.Exists(Path.Combine(App.Mailbox,f))?File.GetLastWriteTimeUtc(Path.Combine(App.Mailbox,f)):DateTime.MinValue;}
 void Tick(object s,EventArgs e){if(Stamp("parar.txt")>stop){ExitThread();return;}Pulse();if(!App.Elevated()&&App.SessionActive()){icon.Visible=false;send=Stamp("enviar.svg");config=Stamp("configurar.svg");return;}icon.Visible=true;if(busy)return;busy=true;try{
  var c=Stamp("configurar.svg");if(c>config&&DateTime.UtcNow-c>TimeSpan.FromMilliseconds(400)){config=c;App.Config();}
  var n=Stamp("enviar.svg");if(n>send&&DateTime.UtcNow-n>TimeSpan.FromMilliseconds(400)){
   string input=Path.Combine(App.Mailbox,"enviar.svg");CutJob job;
   try{job=Geometry.ReadSvg(input);}catch(IOException){return;}
   send=n;string pending=Path.Combine(App.Mailbox,"pronto.svg");File.Copy(input,pending,true);
   App.Log("Selecao preparada: "+job.Width.ToString("0.###",CultureInfo.InvariantCulture)+" x "+job.Height.ToString("0.###",CultureInfo.InvariantCulture)+" mm, "+job.Cuts+" contornos.");
   CameraLink.Send();
  }
 }catch(Exception ex){send=Stamp("enviar.svg");config=Stamp("configurar.svg");Report(ex);}finally{busy=false;}}
 protected override void ExitThreadCore(){timer.Stop();timer.Dispose();if(App.Elevated())try{File.Delete(Path.Combine(App.Home,"sessao-ativa.txt"));}catch{}App.Log("RC7 encerrado. PID="+Process.GetCurrentProcess().Id);icon.Visible=false;icon.Dispose();base.ExitThreadCore();}
}
static class CameraLink {
 [DllImport("user32.dll",CharSet=CharSet.Ansi)]static extern IntPtr FindWindow(string c,string title);
 [DllImport("user32.dll",SetLastError=true)]static extern IntPtr SendMessageTimeout(IntPtr h,uint msg,UIntPtr w,IntPtr l,uint flags,uint time,out UIntPtr result);
 [DllImport("user32.dll")]static extern bool ShowWindow(IntPtr h,int command);
 [DllImport("user32.dll")]static extern bool SetForegroundWindow(IntPtr h);
 [DllImport("user32.dll")]static extern uint GetWindowThreadProcessId(IntPtr h,out uint pid);
 [DllImport("kernel32.dll",SetLastError=true)]static extern IntPtr OpenProcess(uint access,bool inherit,uint pid);
 [DllImport("advapi32.dll",SetLastError=true)]static extern bool OpenProcessToken(IntPtr p,uint access,out IntPtr token);
 [DllImport("advapi32.dll",SetLastError=true)]static extern bool GetTokenInformation(IntPtr t,int c,out int value,int length,out int needed);
 [DllImport("kernel32.dll")]static extern bool CloseHandle(IntPtr h);
 static bool? IsElevated(IntPtr window){uint pid;GetWindowThreadProcessId(window,out pid);IntPtr p=OpenProcess(0x1000,false,pid),t;int v,n;if(p==IntPtr.Zero)return null;try{if(!OpenProcessToken(p,8,out t))return null;try{return GetTokenInformation(t,20,out v,4,out n)?(bool?)(v!=0):null;}finally{CloseHandle(t);}}finally{CloseHandle(p);}}
 public static void Send(){
  string input=Path.Combine(App.Mailbox,"pronto.svg");var job=Geometry.ReadSvg(input);
  if(!App.Elevated()){App.Session("send");return;}
  IntPtr h=FindWindow("#32770","CameraCut");
  if(h==IntPtr.Zero){string cam=App.Camera();if(!File.Exists(cam))throw new Exception("CameraCut nao encontrado. Abra as configuracoes e escolha o CameraCut.exe instalado.");Process.Start(new ProcessStartInfo(cam){UseShellExecute=true,WorkingDirectory=Path.GetDirectoryName(cam),WindowStyle=ProcessWindowStyle.Hidden});for(int i=0;i<80&&h==IntPtr.Zero;i++){Thread.Sleep(125);h=FindWindow("#32770","CameraCut");}}
  if(h==IntPtr.Zero)throw new Exception("Abra o CameraCut e feche suas janelas de aviso. Depois clique em Enviar novamente.");
  if(IsElevated(h)==true&&!App.Elevated()){App.Elevate("--deliver");return;}
  string path=Path.Combine(Path.GetTempPath(),"skycutexport.job");if(File.Exists(path))File.Copy(path,Path.Combine(App.Mailbox,"skycutexport-anterior.job"),true);
  File.WriteAllText(path,job.Text,Encoding.ASCII);UIntPtr result;IntPtr ok=SendMessageTimeout(h,1124,UIntPtr.Zero,IntPtr.Zero,2,5000,out result);
  if(ok==IntPtr.Zero){int err=Marshal.GetLastWin32Error();if(err==5&&!App.Elevated()){App.Elevate("--deliver");return;}throw new Exception("CameraCut nao respondeu (Windows "+err+"). Confira se abriu algum aviso no CameraCut. Nao foi enviado um segundo pedido automaticamente.");}
  ShowWindow(h,6);App.Log("Arquivo entregue ao CameraCut. Janela principal minimizada.");
 }
}
class IniFile {
 public List<string> Lines;Encoding enc;
 public IniFile(string path){enc=Encoding.Default;byte[] b=File.ReadAllBytes(path);if(b.Length>=3&&b[0]==239&&b[1]==187&&b[2]==191)enc=new UTF8Encoding(true);using(var r=new StreamReader(path,enc,true))Lines=r.ReadToEnd().Split(new[]{"\r\n","\n"},StringSplitOptions.None).ToList();}
 public string Get(string section,string key,string fallback){bool active=false;foreach(string raw in Lines){string l=raw.Trim();if(l.StartsWith("[")){active=l.Equals("["+section+"]",StringComparison.OrdinalIgnoreCase);continue;}if(active){int n=l.IndexOf('=');if(n>=0&&l.Substring(0,n).Trim().Equals(key,StringComparison.OrdinalIgnoreCase))return l.Substring(n+1).Trim();}}return fallback;}
 public void Set(string section,string key,string value){int start=-1,end=Lines.Count;for(int i=0;i<Lines.Count;i++){string l=Lines[i].Trim();if(l.Equals("["+section+"]",StringComparison.OrdinalIgnoreCase)){start=i;continue;}if(start>=0&&l.StartsWith("[")){end=i;break;}}if(start<0){Lines.Add("["+section+"]");Lines.Add(key+"="+value);return;}for(int i=start+1;i<end;i++){int n=Lines[i].IndexOf('=');if(n>=0&&Lines[i].Substring(0,n).Trim().Equals(key,StringComparison.OrdinalIgnoreCase)){Lines[i]=key+"="+value;return;}}Lines.Insert(end,key+"="+value);}
 public void Save(string path){string bak=path+".bannercut-"+DateTime.Now.ToString("yyyyMMddHHmmssfff")+".bak";string temp=path+".bannercut.tmp";File.WriteAllText(temp,string.Join("\r\n",Lines.ToArray()),enc);try{File.Replace(temp,path,bak);}finally{if(File.Exists(temp))File.Delete(temp);}}
}
class CutSettings:Form {
 string path;IniFile ini;CheckBox offset,close,outline,dashed;NumericUpDown offValue,closeValue,length,space;ComboBox language;
 Dictionary<Control,string[]> translations=new Dictionary<Control,string[]>();
 void Words(Control c,string pt,string en){translations[c]=new[]{pt,en};}
 void Translate(){bool en=language.SelectedIndex==1;foreach(var p in translations)p.Key.Text=p.Value[en?1:0];Text=en?"BannerCut - CameraCut settings":"BannerCut - Configuracoes do CameraCut";}
 public CutSettings(string camera){
  path=Path.Combine(Path.GetDirectoryName(camera),"CameraCut.cfg");if(!File.Exists(path))throw new Exception("CameraCut.cfg nao encontrado junto ao CameraCut.exe.");ini=new IniFile(path);
  Text="BannerCut - Configuracoes do CameraCut";Width=480;Height=390;FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;StartPosition=FormStartPosition.CenterScreen;
  offset=Check("KnifeOffset", "OffsetEn",20,25);offValue=Number("Offset","0.35",210,25);
  close=Check("Close", "CloseEn",20,65);closeValue=Number("Close","0.40",210,65);
  outline=Check("CutOutline", "CutOutlineEn",20,105);dashed=Check("DashedLine", "DashedEn",20,145);
  Label("Comprimento",20,185);length=Number("LineLen","0.40",210,185);Label("Espacamento",20,225);space=Number("LineSpace","0.40",210,225);
  Label("Idioma desta janela",20,265);language=new ComboBox{Left=210,Top=260,Width=180,DropDownStyle=ComboBoxStyle.DropDownList};language.Items.Add("Portugues (Brasil)");language.Items.Add("English");string pref=Path.Combine(App.Home,"ui-language.txt");language.SelectedIndex=File.Exists(pref)&&File.ReadAllText(pref).Trim()=="en"?1:0;Controls.Add(language);
  var save=new Button{Text="Salvar",Left=250,Top=305,Width=90};save.Click+=(s,e)=>Save();Controls.Add(save);var cancel=new Button{Text="Cancelar",Left=350,Top=305,Width=90,DialogResult=DialogResult.Cancel};Controls.Add(cancel);CancelButton=cancel;
  Words(offset,"Compensacao da lamina","Knife offset");Words(close,"Fechamento do corte","Close");Words(outline,"Cortar contornos","Cut outline");Words(dashed,"Corte tracejado","Dashed line");Words(save,"Salvar","Save");Words(cancel,"Cancelar","Cancel");
  foreach(Control c in Controls){if(c is Label){if(c.Text=="Comprimento")Words(c,"Comprimento (mm)","Dash length (mm)");if(c.Text=="Espacamento")Words(c,"Espacamento (mm)","Gap (mm)");if(c.Text=="Idioma desta janela")Words(c,"Idioma desta janela","This window's language");}}
  language.SelectedIndexChanged+=(s,e)=>{Directory.CreateDirectory(App.Home);File.WriteAllText(pref,language.SelectedIndex==1?"en":"pt-BR");Translate();};Translate();
 }
 void Label(string text,int x,int y){Controls.Add(new Label{Text=text,Left=x,Top=y,Width=180});}
 CheckBox Check(string name,string key,int x,int y){var c=new CheckBox{Text=name,Left=x,Top=y,Width=180,Checked=ini.Get("SETUP",key,"0")=="1"};Controls.Add(c);return c;}
 NumericUpDown Number(string key,string def,int x,int y){decimal v;if(!decimal.TryParse(ini.Get("SETUP",key,def),NumberStyles.Number,CultureInfo.InvariantCulture,out v))v=decimal.Parse(def,CultureInfo.InvariantCulture);var n=new NumericUpDown{Left=x,Top=y,Width=110,DecimalPlaces=2,Increment=0.05m,Maximum=100,Minimum=0,Value=Math.Max(0,Math.Min(100,v))};Controls.Add(n);return n;}
 void Save(){try{
  if(Process.GetProcessesByName("CameraCut").Length>0){MessageBox.Show("Feche o CameraCut antes de salvar. Os valores serao usados quando ele abrir novamente.","BannerCut");return;}
  ini=new IniFile(path);
  ini.Set("SETUP","OffsetEn",offset.Checked?"1":"0");ini.Set("SETUP","CloseEn",close.Checked?"1":"0");ini.Set("SETUP","CutOutlineEn",outline.Checked?"1":"0");ini.Set("SETUP","DashedEn",dashed.Checked?"1":"0");
  ini.Set("SETUP","Offset",offValue.Value.ToString("0.00",CultureInfo.InvariantCulture));ini.Set("SETUP","Close",closeValue.Value.ToString("0.00",CultureInfo.InvariantCulture));ini.Set("SETUP","LineLen",length.Value.ToString("0.00",CultureInfo.InvariantCulture));ini.Set("SETUP","LineSpace",space.Value.ToString("0.00",CultureInfo.InvariantCulture));
  ini.Save(path);Close();
 }catch(UnauthorizedAccessException){App.Error(new Exception("Sem acesso para salvar CameraCut.cfg. Os valores continuam nesta janela; nao e necessario preenche-los novamente."));}catch(Exception ex){App.Error(ex);}}
}

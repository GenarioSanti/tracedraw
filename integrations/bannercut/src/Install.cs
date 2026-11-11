using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Web.Script.Serialization;

static class Install {
 public static string PrepareDesktopSettings(string roaming){
  string dir=Path.Combine(roaming,"TraceDraw");Directory.CreateDirectory(dir);
  string settings=Path.Combine(dir,"settings.json");
  if(!File.Exists(settings))File.WriteAllText(settings,"{}",new UTF8Encoding(false));
  return settings;
 }
 public static void Bundled(string trace){
  if(!File.Exists(trace))throw new Exception("Executavel do TraceDraw nao encontrado.");
  if(Process.GetProcessesByName("tracedraw").Length>0)throw new Exception("Salve seus desenhos e feche o TraceDraw antes de concluir a integracao. Depois use Configurar integracao no menu Iniciar.");
  string camera=App.Camera();
  if(!File.Exists(camera))using(var dialog=new OpenFileDialog{Title="Localizar CameraCut.exe",Filter="CameraCut.exe|CameraCut.exe"}){
   if(dialog.ShowDialog()!=DialogResult.OK)throw new Exception("Integracao pendente. Use Configurar integracao no menu Iniciar quando o CameraCut estiver instalado.");
   camera=dialog.FileName;
  }
  Directory.CreateDirectory(App.Mailbox);
  File.WriteAllText(Path.Combine(App.Mailbox,"parar.txt"),DateTime.UtcNow.ToString("O"));
  for(int attempt=0;attempt<40&&Process.GetProcessesByName("BannerCutIntegrado").Any(p=>p.Id!=Process.GetCurrentProcess().Id);attempt++)System.Threading.Thread.Sleep(250);
  string settings=PrepareDesktopSettings(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
  Do(settings,camera,trace);
 }
 public static readonly string[] Commands={"menu.macro.SSPrintBannerCut|BannerCut|Enviar","menu.macro.SSPrintBannerCut|BannerCut|Marcas","menu.macro.SSPrintBannerCut|BannerCut|Configurar","menu.macro.SSPrintBannerCut|BannerCut|Limite"};
 static readonly string[] DefaultBar={"new","open","save","print","sep","cut","copy","paste","delete","sep","undo","redo","sep","import","export","publish_pdf","sep","zoom_levels","fullscreen","sep","rulers","grid","guidelines","sep","snap_off","snap_to","sep","options","dockers","sep","launch","sep","ai"};
 public static string Resource(string name){using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream(name)){if(stream==null)throw new Exception("Recurso ausente: "+name);using(var r=new StreamReader(stream))return r.ReadToEnd();}}
 public static string FindSettings(){
  var found=new List<string>();string local=Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),packages=Path.Combine(local,"Packages");
  if(Directory.Exists(packages))foreach(var dir in Directory.GetDirectories(packages,"*TraceDraw*")){string p=Path.Combine(dir,@"LocalCache\Roaming\TraceDraw\settings.json");if(File.Exists(p))found.Add(p);}
  string normal=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),"TraceDraw","settings.json");if(File.Exists(normal))found.Add(normal);
  if(found.Count==1)return found[0];return "";
 }
 public static void EditSettings(string path,bool add){
  var serializer=new JavaScriptSerializer{MaxJsonLength=32000000};
  var data=serializer.DeserializeObject(File.ReadAllText(path)) as Dictionary<string,object>;if(data==null)throw new Exception("settings.json invalido.");
  object v;Dictionary<string,object> bars;
  if(data.TryGetValue("bar_items",out v)){bars=v as Dictionary<string,object>;if(bars==null)throw new Exception("Configuracao da barra nao reconhecida.");}else bars=new Dictionary<string,object>();
  var list=new List<string>();
  if(bars.TryGetValue("standard",out v)){var a=v as object[];if(a==null)throw new Exception("Lista da barra nao reconhecida.");list.AddRange(a.Select(x=>Convert.ToString(x)));}else list.AddRange(DefaultBar);
  list.RemoveAll(x=>Commands.Contains(x));
  if(add){list.AddRange(Commands);}bars["standard"]=list.ToArray();data["bar_items"]=bars;
  string temp=path+".bannercut-"+Guid.NewGuid().ToString("N")+".tmp",backup=path+".bannercut-"+DateTime.Now.ToString("yyyyMMdd-HHmmssfff")+".bak";
  File.WriteAllText(temp,serializer.Serialize(data),new UTF8Encoding(false));try{File.Replace(temp,path,backup);}finally{if(File.Exists(temp))File.Delete(temp);}
 }
 public static void Shortcut(string path,string exe,string args){
  object shell=null,link=null;try{shell=Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));link=shell.GetType().InvokeMember("CreateShortcut",BindingFlags.InvokeMethod,null,shell,new object[]{path});Type t=link.GetType();t.InvokeMember("TargetPath",BindingFlags.SetProperty,null,link,new object[]{exe});t.InvokeMember("Arguments",BindingFlags.SetProperty,null,link,new object[]{args});t.InvokeMember("WorkingDirectory",BindingFlags.SetProperty,null,link,new object[]{Path.GetDirectoryName(exe)});t.InvokeMember("Description",BindingFlags.SetProperty,null,link,new object[]{"BannerCut para TraceDraw - SS Print"});t.InvokeMember("Save",BindingFlags.InvokeMethod,null,link,null);}finally{if(link!=null)Marshal.ReleaseComObject(link);if(shell!=null)Marshal.ReleaseComObject(shell);}
 }
 public static string Do(string settings,string camera,string trace){
  if(Process.GetProcessesByName("tracedraw").Length>0)throw new Exception("Salve seus desenhos e feche o TraceDraw. A instalacao nao fecha seus documentos automaticamente.");
  if(!File.Exists(settings)||Path.GetFileName(settings)!="settings.json")throw new Exception("Escolha o settings.json da instalacao do TraceDraw que deseja usar.");
  if(!File.Exists(camera)||!Path.GetFileName(camera).Equals("CameraCut.exe",StringComparison.OrdinalIgnoreCase))throw new Exception("Escolha o CameraCut.exe instalado neste computador.");
  if(Process.GetProcessesByName("BannerCutIntegrado").Any(p=>p.Id!=Process.GetCurrentProcess().Id))throw new Exception("Encerre a integracao anterior pelo icone junto ao relogio antes de atualizar.");
  Directory.CreateDirectory(App.Home);Directory.CreateDirectory(App.Mailbox);string target=Path.Combine(App.Home,"BannerCutIntegrado.exe");
  if(!Path.GetFullPath(App.Exe).Equals(Path.GetFullPath(target),StringComparison.OrdinalIgnoreCase))File.Copy(App.Exe,target,true);
  File.WriteAllText(Path.Combine(App.Home,"LEIA-ME.txt"),Resource("Guia"),new UTF8Encoding(true));
  string source=Resource("Macro").Replace("__MAILBOX__",App.Mailbox.Replace("\"","\"\""));
  File.WriteAllText(Path.Combine(App.Home,"BannerCut.bas"),source,new UTF8Encoding(true));
  string macroDir=Path.Combine(Path.GetDirectoryName(settings),"macros");Directory.CreateDirectory(macroDir);string macro=Path.Combine(macroDir,"SSPrintBannerCut.gms"),tmp=Path.Combine(macroDir,Guid.NewGuid().ToString("N")+".tmp");
  Gms.Write(tmp,source);if(File.Exists(macro))File.Copy(macro,macro+".bak-"+DateTime.Now.ToString("yyyyMMddHHmmssfff"),false);File.Copy(tmp,macro,true);File.Delete(tmp);
  EditSettings(settings,true);
  File.WriteAllText(Path.Combine(App.Home,"settings-path.txt"),settings);File.WriteAllText(Path.Combine(App.Home,"camera-path.txt"),camera);if(File.Exists(trace))File.WriteAllText(Path.Combine(App.Home,"tracedraw-path.txt"),trace);
  string desktop=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),"TraceDraw com BannerCut.lnk");Shortcut(desktop,target,"--start");
  string startup=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup),"BannerCut para TraceDraw.lnk");Shortcut(startup,target,"--watch");
  string startMenu=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs),"BannerCut para TraceDraw");Directory.CreateDirectory(startMenu);Shortcut(Path.Combine(startMenu,"Iniciar integracao.lnk"),target,"--watch");Shortcut(Path.Combine(startMenu,"Desativar integracao.lnk"),target,"--uninstall");
  Process.Start(new ProcessStartInfo(target,"--watch"){UseShellExecute=true,WindowStyle=ProcessWindowStyle.Hidden});return target;
 }
 public static void Uninstall(){
  if(Process.GetProcessesByName("tracedraw").Length>0)throw new Exception("Feche o TraceDraw antes de desativar a integracao.");
  if(MessageBox.Show("Remover os quatro comandos, a macro e o inicio automatico? Seus desenhos, CameraCut e backups serao preservados.","BannerCut",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;
  string p=Path.Combine(App.Home,"settings-path.txt");if(File.Exists(p)){string settings=File.ReadAllText(p);if(File.Exists(settings)){EditSettings(settings,false);string macro=Path.Combine(Path.GetDirectoryName(settings),"macros","SSPrintBannerCut.gms");if(File.Exists(macro))File.Move(macro,macro+".desativado-"+DateTime.Now.ToString("yyyyMMddHHmmssfff"));}}
  foreach(string link in new[]{Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup),"BannerCut para TraceDraw.lnk"),Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),"TraceDraw com BannerCut.lnk")})if(File.Exists(link))File.Delete(link);
  MessageBox.Show("Comandos e inicio automatico desativados. Encerre o componente pelo icone junto ao relogio. A pasta de instalacao foi mantida para preservar logs e backups.","BannerCut");
 }
}
class Setup:Form {
 TextBox settings,camera,trace;Button install;
 public Setup(){
  Text="Instalar BannerCut integrado ao TraceDraw - SS Print";Width=720;Height=440;FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;StartPosition=FormStartPosition.CenterScreen;
  Controls.Add(new Label{Left=20,Top=15,Width=665,Height=50,Text="Adiciona Enviar, Marcas, Configurar e Limite na barra do TraceDraw.\r\nSalve seus desenhos e feche o TraceDraw antes de instalar."});
  settings=Row("Configuracoes do TraceDraw",75,Install.FindSettings(),"Configuracoes|settings.json");camera=Row("CameraCut instalado",145,App.Camera(),"CameraCut|CameraCut.exe");trace=Row("TraceDraw.exe (opcional, para o atalho abrir o editor)",215,"","TraceDraw|tracedraw.exe");
  foreach(var p in Process.GetProcessesByName("tracedraw"))try{trace.Text=p.MainModule.FileName;break;}catch{}
  Controls.Add(new Label{Left=20,Top=285,Width=665,Height=45,Text="Instalacao por usuario. Um componente inicia com o Windows para atender aos botoes.\r\nNao inclui CameraCut nem altera o IP ou a conexao da plotter."});
  install=new Button{Left=485,Top=345,Width=190,Height=34,Text="Instalar integracao"};install.Click+=(s,e)=>{install.Enabled=false;try{string dest=Install.Do(settings.Text,camera.Text,trace.Text);MessageBox.Show("Instalado. Reabra o TraceDraw. Os comandos BannerCut.Enviar, BannerCut.Marcas, BannerCut.Configurar e BannerCut.Limite ficam no fim da barra superior.\r\nPrimeiro teste: confira a previa no CameraCut.\r\nArquivos: "+Path.GetDirectoryName(dest),"BannerCut");Close();}catch(Exception ex){App.Error(ex);}finally{install.Enabled=true;}};Controls.Add(install);
 }
 TextBox Row(string label,int y,string value,string filter){Controls.Add(new Label{Left=20,Top=y,Width=650,Text=label});var t=new TextBox{Left=20,Top=y+23,Width=560,Text=value};Controls.Add(t);var b=new Button{Left=590,Top=y+21,Width=85,Text="Localizar"};b.Click+=(s,e)=>{using(var d=new OpenFileDialog{Filter=filter,CheckFileExists=true})if(d.ShowDialog()==DialogResult.OK)t.Text=d.FileName;};Controls.Add(b);return t;}
}

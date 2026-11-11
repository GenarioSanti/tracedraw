using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Web.Script.Serialization;
static class Tests {
 static void Check(bool ok,string message){if(!ok)throw new Exception("TESTE FALHOU: "+message);}
 public static void Run(string dir){
  Directory.CreateDirectory(dir);var passed=new List<string>();
  string profile=Path.Combine(dir,"desktop-profile");
  string desktopSettings=Install.PrepareDesktopSettings(profile);
  File.WriteAllText(desktopSettings,"{\"existing\":42}");
  Check(Install.PrepareDesktopSettings(profile)==desktopSettings&&File.ReadAllText(desktopSettings)=="{\"existing\":42}","bundled install preserves existing desktop preferences");
  passed.Add("Bundled installer creates a desktop profile and preserves existing preferences.");
  DateTime now=DateTime.UtcNow;Check(App.FreshSession(now,now),"sessao recente");Check(!App.FreshSession(now.AddSeconds(-31),now),"sessao expirada");Check(!App.FreshSession(now.AddSeconds(1),now),"relogio futuro");
  if(!App.Elevated()){
   string previousHome=App.Home;App.Home=Path.Combine(dir,"broker-isolado");Directory.CreateDirectory(App.Home);
   try{using(var broker=new Broker()){
    bool exited=false;broker.ThreadExit+=(s,e)=>{exited=true;};
    var tick=typeof(Broker).GetMethod("Tick",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
    string pulse=Path.Combine(App.Home,"sessao-ativa.txt");File.WriteAllText(pulse,"test");tick.Invoke(broker,new object[]{null,EventArgs.Empty});Check(!exited,"auxiliar normal permanece ativo durante sessao autorizada");
    File.SetLastWriteTimeUtc(pulse,DateTime.UtcNow.AddSeconds(-40));tick.Invoke(broker,new object[]{null,EventArgs.Empty});Check(!exited,"auxiliar retoma quando sessao expira");
    File.WriteAllText(Path.Combine(App.Mailbox,"parar.txt"),"stop");tick.Invoke(broker,new object[]{null,EventArgs.Empty});Check(exited,"encerramento explicito encerra auxiliar");
   }}finally{App.Home=previousHome;}
   passed.Add("RC6: auxiliar permanece ativo durante sessao autorizada, retoma apos expiracao e encerra somente por pedido explicito (teste isolado sem CameraCut).");
  }
  string svg="<svg xmlns='http://www.w3.org/2000/svg' width='210mm' height='297mm' viewBox='0 0 210 297'><path data-name='BC_SELECTED_1' d='M80 177L130 177L130 127L80 127Z'/><path data-name='BC_MARK_X00_Y00' d='M86 113L83 113M130 191L144 191L144 177M130 113L144 113L144 127M80 113L66 113L66 127M80 191L66 191L66 177'/></svg>";
  string input=Path.Combine(dir,"teste.svg");File.WriteAllText(input,svg);var job=Geometry.ReadSvg(input);
  Check(job.Width==78&&job.Height==78,"marcas 78x78");Check(job.Paths.Count==1&&job.Marks.Count==5,"contornos e marcas separados");
  Check(job.Text.Contains("PU-1000 -1000;")&&job.Text.Contains("PD1000 1000;"),"quadrado 50x50 em 40 unidades/mm");
  File.WriteAllText(Path.Combine(dir,"teste.job"),job.Text);passed.Add("Quadrado 50x50, marcas 78x78, grupos SP3/SP1 e escala 40 unidades/mm.");
  string image="<image href='data:image/jpeg;base64,"+new string('A',17000000)+"'/>";
  File.WriteAllText(input,svg.Replace("</svg>",image+"</svg>"));Check(Geometry.ReadSvg(input).Text==job.Text,"imagem grande nao selecionada nao altera corte");
  File.WriteAllText(input,svg.Replace("</svg>",image.Replace("<image ","<image data-name='BC_SELECTED_2' ")+"</svg>"));
  bool rejectedImage=false;try{Geometry.ReadSvg(input);}catch(Exception ex){rejectedImage=ex.Message.Contains("Selecao contem image");}Check(rejectedImage,"imagem selecionada continua bloqueada");
  File.WriteAllText(input,svg.Replace("</svg>","<g data-name='BC_SELECTED_2'>"+image+"</g></svg>"));
  rejectedImage=false;try{Geometry.ReadSvg(input);}catch(Exception ex){rejectedImage=ex.Message.Contains("Selecao contem image");}Check(rejectedImage,"imagem em grupo selecionado continua bloqueada");
  passed.Add("RC7: imagem incorporada acima de 16 MB fora da selecao ignorada; imagem selecionada direta ou em grupo bloqueada; job identico ao arquivo sem imagem.");
  string reference=Path.Combine(dir,"referencia.job");if(File.Exists(reference)){
   var a=System.Text.RegularExpressions.Regex.Matches(File.ReadAllText(reference),@"(PU|PD)(-?\d+)\s+(-?\d+);").Cast<System.Text.RegularExpressions.Match>().ToArray();
   var b=System.Text.RegularExpressions.Regex.Matches(job.Text,@"(PU|PD)(-?\d+)\s+(-?\d+);").Cast<System.Text.RegularExpressions.Match>().ToArray();
   Check(a.Length==b.Length,"quantidade de pontos da referencia");for(int i=0;i<a.Length;i++)Check(a[i].Groups[1].Value==b[i].Groups[1].Value&&int.Parse(a[i].Groups[2].Value)+3==int.Parse(b[i].Groups[2].Value)&&a[i].Groups[3].Value==b[i].Groups[3].Value,"pontos da referencia");passed.Add("Mesmos pontos do job de referencia validado, com deslocamento uniforme de 0,075 mm em X.");
  }
  File.WriteAllText(input,svg.Replace("</svg>","<path d='M0 0L500 500'/></svg>"));Check(Geometry.ReadSvg(input).Text==job.Text,"objeto nao selecionado ignorado");passed.Add("Objetos nao selecionados ignorados.");
  File.WriteAllText(input,svg.Replace("data-name='BC_SELECTED_1'","data-name='nao_selecionado'"));bool blocked=false;try{Geometry.ReadSvg(input);}catch{blocked=true;}Check(blocked,"ausencia de corte bloqueada");
  File.WriteAllText(input,svg.Replace("M80 177L130 177L130 127L80 127Z","M0 0L1 1"));blocked=false;try{Geometry.ReadSvg(input);}catch{blocked=true;}Check(blocked,"contorno fora das marcas bloqueado");
  File.WriteAllText(input,svg.Replace("<path data-name='BC_SELECTED_1'","<path transform='scale(2)' data-name='BC_SELECTED_1'"));blocked=false;try{Geometry.ReadSvg(input);}catch{blocked=true;}Check(blocked,"transformacao SVG nao tratada bloqueada");
  var curve=Geometry.Path("M0 0 C0 50 50 50 50 0 Q25 -20 0 0 Z");Check(curve.Count==1&&curve[0].Count>50,"curvas C/Q subdivididas");passed.Add("Curvas C/Q subdivididas; bloqueios para corte ausente, fora das marcas e transformacoes nao suportadas.");
  File.WriteAllText(input,"<!DOCTYPE svg [<!ENTITY x SYSTEM 'file:///does-not-exist'>]>"+svg);blocked=false;try{Geometry.ReadSvg(input);}catch{blocked=true;}Check(blocked,"DTD externo bloqueado");passed.Add("Entidades XML externas bloqueadas.");
  string iniPath=Path.Combine(dir,"CameraCut.cfg");File.WriteAllText(iniPath,"[SETUP]\r\nOffset=0.35\r\nClose=0.40\r\n[MARK]\r\nMarkSpc=14\r\n[NETWORK]\r\nIP=192.0.2.1\r\n");var ini=new IniFile(iniPath);ini.Set("SETUP","Offset","0.45");ini.Save(iniPath);var reread=new IniFile(iniPath);Check(reread.Get("MARK","MarkSpc","")=="14"&&reread.Get("NETWORK","IP","")=="192.0.2.1"&&reread.Get("SETUP","Offset","")=="0.45","INI preserva campos");Check(Directory.GetFiles(dir,"CameraCut.cfg.bannercut-*.bak").Length>0,"backup INI");passed.Add("Configuracoes preservam secoes desconhecidas e fazem backup.");
  string settings=Path.Combine(dir,"settings.json");File.WriteAllText(settings,"{\"bar_items\":{\"standard\":[\"new\",\"custom_existing\"]},\"other\":42}");Install.EditSettings(settings,true);Install.EditSettings(settings,true);var ser=new JavaScriptSerializer();var cfg=(Dictionary<string,object>)ser.DeserializeObject(File.ReadAllText(settings));var bars=(Dictionary<string,object>)cfg["bar_items"];var list=(object[])bars["standard"];Check(list.Length==6&&list.Contains("custom_existing")&&(int)cfg["other"]==42,"barra preservada sem duplicacao");Install.EditSettings(settings,false);cfg=(Dictionary<string,object>)ser.DeserializeObject(File.ReadAllText(settings));list=(object[])((Dictionary<string,object>)cfg["bar_items"])["standard"];Check(list.Length==2,"desinstalacao preserva barra");passed.Add("Registro dos quatro comandos idempotente; remocao preserva os comandos existentes.");
  string gms=Path.Combine(dir,"SSPrintBannerCut.gms");Gms.Write(gms,Install.Resource("Macro").Replace("__MAILBOX__",dir));byte[] bytes=File.ReadAllBytes(gms);Check(bytes.Length>512&&bytes[0]==0xD0&&bytes[1]==0xCF,"projeto GMS estruturado");passed.Add("Projeto GMS gerado com armazenamento composto do Windows.");
  Check(Gms.ReadSourceForTest(gms)==Install.Resource("Macro").Replace("__MAILBOX__",dir),"fonte VBA recuperado integralmente do GMS");passed.Add("Leitura independente do arquivo composto e descompressao recuperam integralmente o codigo VBA.");
  File.WriteAllText(Path.Combine(dir,"RESULTADO.txt"),string.Join("\r\n",passed.ToArray())+"\r\nPENDENTE: carregar e executar os quatro botoes na interface do TraceDraw; validar o novo fluxo na plotter.\r\n",new UTF8Encoding(true));
 }
}


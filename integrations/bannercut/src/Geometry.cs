using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using System.Xml.Linq;

struct Pt {
 public double X,Y;
 public Pt(double x,double y){X=x;Y=y;}
 public static Pt Mid(Pt a,Pt b){return new Pt((a.X+b.X)/2,(a.Y+b.Y)/2);}
}
class CutJob {
 public string Text;
 public double Width,Height;
 public int Cuts;
 public List<List<Pt>> Marks=new List<List<Pt>>(),Paths=new List<List<Pt>>();
}
static class Geometry {
 static readonly CultureInfo Inv=CultureInfo.InvariantCulture;
 static double N(string x){double n=double.Parse(x,Inv);if(double.IsInfinity(n)||double.IsNaN(n)||Math.Abs(n)>1000000)throw new Exception("Coordenada fora do limite.");return n;}
 static double Dist(Pt p,Pt a,Pt b){double x=b.X-a.X,y=b.Y-a.Y,l=x*x+y*y;double t=l==0?0:Math.Max(0,Math.Min(1,((p.X-a.X)*x+(p.Y-a.Y)*y)/l));return Math.Sqrt(Math.Pow(p.X-a.X-t*x,2)+Math.Pow(p.Y-a.Y-t*y,2));}
 static void Cubic(List<Pt> p,Pt a,Pt b,Pt c,Pt d,int depth){
  if(p.Count>200000)throw new Exception("Curva muito complexa.");
  if(Math.Max(Dist(b,a,d),Dist(c,a,d))<=0.01){p.Add(d);return;}
  if(depth>18)throw new Exception("Nao foi possivel aproximar a curva com precisao.");
  Pt ab=Pt.Mid(a,b),bc=Pt.Mid(b,c),cd=Pt.Mid(c,d),u=Pt.Mid(ab,bc),v=Pt.Mid(bc,cd),w=Pt.Mid(u,v);
  Cubic(p,a,ab,u,w,depth+1);Cubic(p,w,v,cd,d,depth+1);
 }
 public static List<List<Pt>> Path(string data){
  const string pattern=@"[a-zA-Z]|[-+]?(?:\d*\.?\d+)(?:[eE][-+]?\d+)?";
  if(Regex.Replace(data,pattern+@"|[\s,]","").Length!=0)throw new Exception("Caminho SVG invalido.");
  string[] ts=Regex.Matches(data,pattern).Cast<Match>().Select(m=>m.Value).ToArray();if(ts.Length>1000000)throw new Exception("Caminho muito complexo.");int i=0;char cmd=' ';Pt at=new Pt(),start=new Pt();List<Pt> p=null;var all=new List<List<Pt>>();
  Func<double> number=()=>{if(i>=ts.Length)throw new Exception("Caminho SVG incompleto.");return N(ts[i++]);};
  while(i<ts.Length){
   if(ts[i].Length==1&&char.IsLetter(ts[i][0]))cmd=ts[i++][0];
   bool rel=char.IsLower(cmd);char c=char.ToUpperInvariant(cmd);Pt origin=at;
   Func<Pt> point=()=>new Pt(number()+(rel?origin.X:0),number()+(rel?origin.Y:0));
   if(c=='M'){at=point();start=at;p=new List<Pt>{at};all.Add(p);cmd=rel?'l':'L';continue;}
   if(p==null)throw new Exception("Caminho sem inicio.");
   if(c=='L'){at=point();p.Add(at);}
   else if(c=='H'){at=new Pt(number()+(rel?origin.X:0),at.Y);p.Add(at);}
   else if(c=='V'){at=new Pt(at.X,number()+(rel?origin.Y:0));p.Add(at);}
   else if(c=='C'){Pt b=point(),cc=point(),d=point();Cubic(p,at,b,cc,d,0);at=d;}
   else if(c=='Q'){Pt q=point(),d=point();Cubic(p,at,new Pt(at.X+2*(q.X-at.X)/3,at.Y+2*(q.Y-at.Y)/3),new Pt(d.X+2*(q.X-d.X)/3,d.Y+2*(q.Y-d.Y)/3),d,0);at=d;}
   else if(c=='Z'){p.Add(start);at=start;cmd=' ';}
   else throw new Exception("Curva SVG nao suportada: "+cmd+". Converta o objeto em curvas.");
   if(p.Count>200000||all.Count>200000)throw new Exception("Limite de pontos excedido.");
  }
  return all.Where(a=>a.Count>1).ToList();
 }
 static double[] Bounds(List<List<Pt>> paths){var a=paths.SelectMany(p=>p).ToArray();if(a.Length==0)throw new Exception("Nenhum contorno encontrado.");return new[]{a.Min(p=>p.X),a.Min(p=>p.Y),a.Max(p=>p.X),a.Max(p=>p.Y)};}
 // Keep image elements for selection validation, but discard their embedded pixels.
 // A page export can contain large, unselected print artwork alongside cut paths.
 static XDocument ReadGeometryDocument(string file){
  const long transferLimit=128000000,geometryLimit=16000000;
  if(new FileInfo(file).Length>transferLimit)throw new Exception("Pagina exportada maior que 128 MB. Reduza as imagens da pagina e tente novamente.");
  var doc=new XDocument();var parents=new Stack<XElement>();long retained=0;int elements=0;
  using(var r=XmlReader.Create(file,new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=transferLimit})){
   while(r.Read()){
    if(r.NodeType==XmlNodeType.Element){
     if(++elements>200000||parents.Count>128)throw new Exception("Estrutura SVG muito complexa.");
     var el=new XElement(XName.Get(r.LocalName,r.NamespaceURI));bool empty=r.IsEmptyElement;
     if(r.MoveToFirstAttribute())do{
      if(r.Prefix=="xmlns"||r.Name=="xmlns")continue;
      if(el.Name.LocalName=="image"&&r.LocalName=="href")continue;
      string value=r.Value;retained+=value.Length;
      if(retained>geometryLimit)throw new Exception("Geometria SVG maior que 16 MB. Simplifique os contornos.");
      el.SetAttributeValue(XName.Get(r.LocalName,r.NamespaceURI),value);
     }while(r.MoveToNextAttribute());
     r.MoveToElement();if(parents.Count==0)doc.Add(el);else parents.Peek().Add(el);
     if(!empty)parents.Push(el);
    }else if(r.NodeType==XmlNodeType.EndElement)parents.Pop();
   }
  }
  return doc;
 }
 public static CutJob ReadSvg(string file){
  XDocument doc=ReadGeometryDocument(file);
  var root=doc.Root;if(root==null||root.Name.LocalName!="svg")throw new Exception("Arquivo de transferencia invalido.");
  string width=(string)root.Attribute("width")??"",height=(string)root.Attribute("height")??"";
  if(!width.EndsWith("mm")||!height.EndsWith("mm"))throw new Exception("A transferencia precisa estar em milimetros.");
  double w=N(width.Substring(0,width.Length-2)),h=N(height.Substring(0,height.Length-2));
  var vb=Regex.Split(((string)root.Attribute("viewBox")??"").Trim(),@"[\s,]+").Select(N).ToArray();
  if(vb.Length!=4||vb[2]<=0||vb[3]<=0)throw new Exception("Escala SVG invalida.");
  double scale=w/vb[2];if(Math.Abs(scale-h/vb[3])>0.000001)throw new Exception("Escala SVG desproporcional.");
  var job=new CutJob();int markObjects=0;
  foreach(var el in root.Descendants()){
   string name=(string)el.Attribute("data-name")??"";bool isMark=name=="BC_MARK_X00_Y00";
   bool selected=el.AncestorsAndSelf().Any(e=>((string)e.Attribute("data-name")??"").StartsWith("BC_SELECTED_",StringComparison.Ordinal));
   if(!isMark&&!selected)continue;
   if(el.Name.LocalName=="g")continue;
   if(el.Name.LocalName!="path")throw new Exception("Selecao contem "+el.Name.LocalName+". Converta textos em curvas e selecione apenas contornos e marcas.");
   if(el.AncestorsAndSelf().Any(e=>e.Attribute("transform")!=null||e.Attribute("clip-path")!=null||e.Attribute("filter")!=null||e.Attribute("mask")!=null))throw new Exception("Separe efeitos, recortes e transformacoes especiais antes de enviar.");
   if(el.AncestorsAndSelf().Any(e=>(string)e.Attribute("display")=="none"||(string)e.Attribute("visibility")=="hidden"))throw new Exception("A selecao contem objetos ocultos.");
   var paths=Path((string)el.Attribute("d")??"");
   paths=paths.Select(p=>p.Select(a=>new Pt((a.X-vb[0])*scale,(vb[1]+vb[3]-a.Y)*scale)).ToList()).ToList();
   if(isMark){markObjects++;job.Marks.AddRange(paths);}else {job.Cuts++;job.Paths.AddRange(paths);}
  }
  if(markObjects!=1||job.Marks.Count!=5)throw new Exception("Selecione um conjunto completo de marcas: quatro Ls e o traco superior.");
  if(job.Paths.Count==0)throw new Exception("Nao ha contornos de corte na selecao.");
  var b=Bounds(job.Marks);var cut=Bounds(job.Paths);job.Width=b[2]-b[0];job.Height=b[3]-b[1];
  if(job.Width<=0||job.Height<=0)throw new Exception("Marcas sem area.");
  if(cut[0]<b[0]-0.025||cut[1]<b[1]-0.025||cut[2]>b[2]+0.025||cut[3]>b[3]+0.025)throw new Exception("Ha contornos fora das marcas. Ajuste as marcas antes de enviar.");
  if(job.Paths.Sum(p=>p.Count)+job.Marks.Sum(p=>p.Count)>200000)throw new Exception("Desenho muito complexo.");
  double cx=(b[0]+b[2])/2,cy=(b[1]+b[3])/2;var lines=new List<string>{"IN;"};
  for(int n=1;n<=8;n++)lines.Add("VS32,"+n+";");lines.Add("WU0;");for(int n=1;n<=8;n++)lines.Add("PW0.350,"+n+";");
  Action<List<List<Pt>>,int> emit=(paths,pen)=>{lines.Add("LT;");lines.Add("SP"+pen+";");foreach(var p in paths){string last="";for(int i=0;i<p.Count;i++){string xy=Math.Floor((p[i].X-cx)*40+0.5).ToString(Inv)+" "+Math.Floor((p[i].Y-cy)*40+0.5).ToString(Inv);if(i==0||last!=xy)lines.Add((i==0?"PU":"PD")+xy+";");last=xy;}}};
  emit(job.Marks,3);emit(job.Paths,1);lines.Add("SP0;");job.Text=string.Join("\r\n",lines.ToArray());return job;
 }
}

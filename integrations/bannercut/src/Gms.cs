using System;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
[ComImport, Guid("0000000B-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IStorage {
 void CreateStream([MarshalAs(UnmanagedType.LPWStr)]string name,uint mode,uint r1,uint r2,out IStream stream);
 void OpenStream([MarshalAs(UnmanagedType.LPWStr)]string name,IntPtr r1,uint mode,uint r2,out IStream stream);
 void CreateStorage([MarshalAs(UnmanagedType.LPWStr)]string name,uint mode,uint r1,uint r2,out IStorage storage);
 void OpenStorage([MarshalAs(UnmanagedType.LPWStr)]string name,IStorage priority,uint mode,IntPtr exclude,uint r,out IStorage storage);
 void CopyTo(uint c,IntPtr ids,IntPtr exclude,IStorage dest);
 void MoveElementTo([MarshalAs(UnmanagedType.LPWStr)]string name,IStorage dest,[MarshalAs(UnmanagedType.LPWStr)]string newName,uint flags);
 void Commit(uint flags);
 void Revert();
 void EnumElements(uint r1,IntPtr r2,uint r3,out IntPtr result);
 void DestroyElement([MarshalAs(UnmanagedType.LPWStr)]string name);
 void RenameElement([MarshalAs(UnmanagedType.LPWStr)]string a,[MarshalAs(UnmanagedType.LPWStr)]string b);
 void SetElementTimes([MarshalAs(UnmanagedType.LPWStr)]string n,IntPtr c,IntPtr a,IntPtr m);
 void SetClass(ref Guid id);
 void SetStateBits(uint bits,uint mask);
 void Stat(out System.Runtime.InteropServices.ComTypes.STATSTG stat,uint flags);
}
static class Gms {
 [DllImport("ole32.dll",CharSet=CharSet.Unicode)]static extern int StgOpenStorage(string name,IStorage priority,uint mode,IntPtr exclude,uint reserved,out IStorage storage);
 public static string ReadSourceForTest(string path){
  IStorage root;Marshal.ThrowExceptionForHR(StgOpenStorage(path,null,0x10,IntPtr.Zero,0,out root));
  try{IStorage vba;root.OpenStorage("VBA",null,0x10,IntPtr.Zero,0,out vba);try{IStream s;vba.OpenStream("BannerCut",IntPtr.Zero,0x10,0,out s);try{
   System.Runtime.InteropServices.ComTypes.STATSTG info;s.Stat(out info,1);byte[] data=new byte[checked((int)info.cbSize)];s.Read(data,data.Length,IntPtr.Zero);
   using(var output=new MemoryStream()){int pos=0;if(data[pos++]!=1)throw new InvalidDataException("VBA signature");while(pos<data.Length){int header=data[pos]|(data[pos+1]<<8);pos+=2;int end=pos+(header&4095)+1;if((header&0xF000)!=0xB000||end>data.Length)throw new InvalidDataException("VBA chunk");while(pos<end){if(data[pos++]!=0)throw new InvalidDataException("Expected literal VBA tokens");for(int i=0;i<8&&pos<end;i++)output.WriteByte(data[pos++]);}}return Encoding.GetEncoding(1252).GetString(output.ToArray());}
  }finally{Marshal.ReleaseComObject(s);}}finally{Marshal.ReleaseComObject(vba);}}finally{Marshal.ReleaseComObject(root);}
 }
 [DllImport("ole32.dll",CharSet=CharSet.Unicode)]static extern int StgCreateDocfile(string name,uint mode,uint reserved,out IStorage storage);
 static byte[] S(string s){return Encoding.GetEncoding(1252).GetBytes(s);}
 static void Record(BinaryWriter b,ushort id,byte[] data){b.Write(id);b.Write(data.Length);b.Write(data);}
 static byte[] Pack(byte[] data){using(var m=new MemoryStream())using(var b=new BinaryWriter(m)){b.Write((byte)1);for(int off=0;off<data.Length;off+=3600){int n=Math.Min(3600,data.Length-off);using(var p=new MemoryStream()){for(int i=0;i<n;i+=8){p.WriteByte(0);p.Write(data,off+i,Math.Min(8,n-i));}byte[] a=p.ToArray();b.Write((ushort)(0xB000|(a.Length-1)));b.Write(a);}}return m.ToArray();}}
 static void Stream(IStorage st,string name,byte[] data){IStream s;st.CreateStream(name,0x1012,0,0,out s);s.Write(data,data.Length,IntPtr.Zero);s.Commit(0);Marshal.ReleaseComObject(s);}
 public static void Write(string path,string source){
  byte[] dir;using(var m=new MemoryStream())using(var b=new BinaryWriter(m)){
   Record(b,1,BitConverter.GetBytes(1));Record(b,2,BitConverter.GetBytes(0x409));Record(b,0x14,BitConverter.GetBytes(0x409));Record(b,3,BitConverter.GetBytes((ushort)1252));
   Record(b,4,S("SSPrintBannerCut"));Record(b,5,new byte[0]);Record(b,0x40,new byte[0]);Record(b,6,new byte[0]);Record(b,0x3D,new byte[0]);Record(b,7,BitConverter.GetBytes(0));Record(b,8,BitConverter.GetBytes(0));
   b.Write((ushort)9);b.Write(4);b.Write(1);b.Write((ushort)0);Record(b,0xC,new byte[0]);Record(b,0x3C,new byte[0]);
   Record(b,0xF,BitConverter.GetBytes((ushort)1));Record(b,0x13,BitConverter.GetBytes((ushort)0xFFFF));
   Record(b,0x19,S("BannerCut"));Record(b,0x47,Encoding.Unicode.GetBytes("BannerCut"));Record(b,0x1A,S("BannerCut"));Record(b,0x32,Encoding.Unicode.GetBytes("BannerCut"));
   Record(b,0x1C,new byte[0]);Record(b,0x48,new byte[0]);Record(b,0x31,BitConverter.GetBytes(0));Record(b,0x1E,BitConverter.GetBytes(0));Record(b,0x2C,BitConverter.GetBytes((ushort)0xFFFF));
   Record(b,0x21,new byte[0]);Record(b,0x2B,new byte[0]);Record(b,0x10,new byte[0]);dir=m.ToArray();
  }
  IStorage root;Marshal.ThrowExceptionForHR(StgCreateDocfile(path,0x1012,0,out root));
  try{IStorage vba;root.CreateStorage("VBA",0x1012,0,0,out vba);try{Stream(vba,"dir",Pack(dir));Stream(vba,"BannerCut",Pack(S(source)));Stream(vba,"_VBA_PROJECT",new byte[]{0xCC,0x61,0xFF,0xFF,0,0,0});vba.Commit(0);}finally{Marshal.ReleaseComObject(vba);}
   Stream(root,"PROJECT",S("ID=\"{00000000-0000-0000-0000-000000000000}\"\r\nModule=BannerCut\r\nName=\"SSPrintBannerCut\"\r\nHelpContextID=\"0\"\r\nVersionCompatible32=\"393222000\"\r\n"));
   using(var m=new MemoryStream()){byte[] a=S("BannerCut");m.Write(a,0,a.Length);m.WriteByte(0);a=Encoding.Unicode.GetBytes("BannerCut");m.Write(a,0,a.Length);m.WriteByte(0);m.WriteByte(0);m.WriteByte(0);m.WriteByte(0);Stream(root,"PROJECTwm",m.ToArray());}root.Commit(0);
  }finally{Marshal.ReleaseComObject(root);}
 }
}

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;

// Independent, Windows-API-only experiment. No Camo assemblies are loaded.
// Frame data is discarded; only counts, format metadata and aggregate values are logged.
static class Native
{
    [DllImport("ole32.dll")] public static extern int CoInitializeEx(IntPtr reserved,uint flags);
    [DllImport("ole32.dll")] public static extern void CoUninitialize();
    [DllImport("mfplat.dll")] public static extern int MFStartup(uint version,uint flags);
    [DllImport("mfplat.dll")] public static extern int MFShutdown();
    [DllImport("mfplat.dll")] public static extern int MFCreateAttributes(out IntPtr attributes,uint initialSize);
    [DllImport("mf.dll")] public static extern int MFEnumDeviceSources(IntPtr attributes,out IntPtr devices,out uint count);
    [DllImport("mfreadwrite.dll")] public static extern int MFCreateSourceReaderFromMediaSource(IntPtr source,IntPtr attributes,out IntPtr reader);
    [DllImport("cfgmgr32.dll",CharSet=CharSet.Unicode)] public static extern uint CM_Locate_DevNodeW(out uint device,string id,uint flags);
    [DllImport("cfgmgr32.dll")] public static extern uint CM_Get_DevNode_Status(out uint status,out uint problem,uint device,uint flags);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate int SetGuid(IntPtr self,ref Guid key,ref Guid value);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate int GetGuid(IntPtr self,ref Guid key,out Guid value);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate int GetU64(IntPtr self,ref Guid key,out ulong value);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate int GetString(IntPtr self,ref Guid key,out IntPtr value,out uint length);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate int Activate(IntPtr self,ref Guid iid,out IntPtr source);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate int Simple(IntPtr self);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate int GetNativeType(IntPtr self,uint stream,uint index,out IntPtr type);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate int SetMediaType(IntPtr self,uint stream,IntPtr reserved,IntPtr type);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate int ReadSample(IntPtr self,uint stream,uint control,out uint actual,out uint flags,out long time,out IntPtr sample);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate int GetBuffer(IntPtr self,out IntPtr buffer);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] public delegate int LockBuffer(IntPtr self,out IntPtr bytes,out uint max,out uint length);
    public static T Method<T>(IntPtr obj,int slot) where T:Delegate => Marshal.GetDelegateForFunctionPointer<T>(Marshal.ReadIntPtr(Marshal.ReadIntPtr(obj),slot*IntPtr.Size));
    public static void Check(int hr,string operation) { if(hr<0) throw new COMException(operation,hr); }
    public static void Release(ref IntPtr obj) {if(obj!=IntPtr.Zero){Marshal.Release(obj);obj=IntPtr.Zero;}}
}

static partial class Program
{
    static Guid SourceType=new("c60ac5fe-252a-478f-a0ef-bc8fa5f7cad3"),VideoCapture=new("8ac3587a-4ae7-42d8-99e0-0a6013eef90f");
    static Guid FriendlyName=new("60d0e559-52f8-4fa2-bbce-acdb34a8ec01"),SymbolicLink=new("58f0aad8-22bf-4f8a-bb3d-d2c4978c6e2f");
    static Guid MediaSource=new("279a808d-aec7-40c8-9c6b-a6b492c78a66"),SubType=new("f7e34c9a-42e8-4714-b74b-cb29d72c35e5"),FrameSize=new("1652c33d-d6b2-4012-b834-72030849a37d");
    const uint VideoStream=0xfffffffc;
    static string Mode="";
    // Resolve the per-machine instance in memory; never ship a captured instance ID.
    static string? SelectedCameraInstanceId;
    static void Log(string kind,object data) => Console.WriteLine(JsonSerializer.Serialize(new{time=DateTimeOffset.Now.ToString("o"),mode=Mode,kind,data}));
    static string Text(IntPtr attributes,ref Guid key)
    {
        var hr=Native.Method<Native.GetString>(attributes,13)(attributes,ref key,out var text,out _);
        if(hr<0)return "";
        try{return Marshal.PtrToStringUni(text)??"";}finally{Marshal.FreeCoTaskMem(text);}
    }
    static IntPtr FindCamera()
    {
        IntPtr attr=IntPtr.Zero,array=IntPtr.Zero,result=IntPtr.Zero;uint count=0;
        try{
            Native.Check(Native.MFCreateAttributes(out attr,1),"MFCreateAttributes");
            Native.Check(Native.Method<Native.SetGuid>(attr,24)(attr,ref SourceType,ref VideoCapture),"Set video source type");
            Native.Check(Native.MFEnumDeviceSources(attr,out array,out count),"MFEnumDeviceSources");
            for(int i=0;i<count;i++){
                var device=Marshal.ReadIntPtr(array,i*IntPtr.Size);var path=Text(device,ref SymbolicLink);
                if(path.Contains("vid_13d3&pid_56ff",StringComparison.OrdinalIgnoreCase)){
                    var parts=path.Split('#');
                    if(parts.Length<3)continue;
                    SelectedCameraInstanceId="USB\\"+parts[1]+"\\"+parts[2];
                    Marshal.AddRef(device);result=device;Log("mf_camera_found",new{name=Text(device,ref FriendlyName),hardwareId="VID_13D3&PID_56FF"});break;
                }
            }
            return result;
        }finally{
            for(int i=0;i<count;i++){var device=Marshal.ReadIntPtr(array,i*IntPtr.Size);Marshal.Release(device);}
            if(array!=IntPtr.Zero)Marshal.FreeCoTaskMem(array);Native.Release(ref attr);
        }
    }
    static object PnpState()
    {
        uint device=0;
        var cr=SelectedCameraInstanceId is null?13u:Native.CM_Locate_DevNodeW(out device,SelectedCameraInstanceId,0);
        uint status=0,problem=0;var statusResult=cr==0?Native.CM_Get_DevNode_Status(out status,out problem,device,0):cr;
        return new{locateResult=cr,statusResult,status="0x"+status.ToString("X"),problem,started=(status&8)!=0};
    }
    static bool CameraStarted()
    {
        return SelectedCameraInstanceId is not null&&Native.CM_Locate_DevNodeW(out var device,SelectedCameraInstanceId,0)==0
            &&Native.CM_Get_DevNode_Status(out var status,out var problem,device,0)==0&&(status&8)!=0&&problem==0;
    }
    static void KeepSourceOpen(int duration)
    {
        var elapsed=Stopwatch.StartNew();
        while(duration==0||elapsed.Elapsed.TotalSeconds<duration){
            IntPtr activation=IntPtr.Zero,source=IntPtr.Zero;
            try{
                activation=FindCamera();
                if(activation==IntPtr.Zero){Thread.Sleep(1000);continue;}
                Native.Check(Native.Method<Native.Activate>(activation,33)(activation,ref MediaSource,out source),"Keeper activate source");
                Native.Check(Native.Method<Native.Simple>(activation,35)(activation),"Keeper detach source");
                Log("keeper_source_open",new{pnp=PnpState(),recordsVideo=false});
                long lastReport=-1;
                while((duration==0||elapsed.Elapsed.TotalSeconds<duration)&&CameraStarted()){
                    long report=elapsed.ElapsedMilliseconds/30000;
                    if(report!=lastReport){lastReport=report;Log("keeper_held",new{pnp=PnpState()});}
                    Thread.Sleep(500);
                }
            }catch(COMException e){Log("keeper_retry",new{message=e.Message,hresult="0x"+e.HResult.ToString("X8")});Thread.Sleep(1000);}
            finally{
                if(source!=IntPtr.Zero)Native.Method<Native.Simple>(source,12)(source);
                Native.Release(ref source);Native.Release(ref activation);
            }
        }
    }
    static object FormatInfo(IntPtr type)
    {
        var subtypeHr=Native.Method<Native.GetGuid>(type,10)(type,ref SubType,out var subtype);
        var sizeHr=Native.Method<Native.GetU64>(type,8)(type,ref FrameSize,out var size);
        return new{subtypeHr,subtype=subtype.ToString(),sizeHr,width=size>>32,height=size&0xffffffff};
    }
    static object SampleStats(IntPtr sample)
    {
        IntPtr buffer=IntPtr.Zero;
        Native.Check(Native.Method<Native.GetBuffer>(sample,41)(sample,out buffer),"ConvertToContiguousBuffer");
        try{
            Native.Check(Native.Method<Native.LockBuffer>(buffer,3)(buffer,out var bytes,out _,out var length),"Lock buffer");
            try{
                var count=(int)Math.Min(length,8*1024*1024);var data=new byte[count];Marshal.Copy(bytes,data,0,count);
                double[] sums=new double[4];int zeros=0;
                for(int i=0;i<count;i++){sums[i%4]+=data[i];if(data[i]==0)zeros++;}
                return new{length,inspected=count,zeroFraction=count>0?(double)zeros/count:0,meanByBytePosition=sums.Select((s,i)=>s/Math.Max(1,(count+3-i)/4)).ToArray()};
            }finally{Native.Method<Native.Simple>(buffer,4)(buffer);}
        }finally{Native.Release(ref buffer);}
    }
    public static int Main(string[] args)
    {
        Mode=args.Length>0?args[0]:"enum";int duration=args.Length>1?int.Parse(args[1]):30;
        if(!new[]{"enum","activate","reader","stream","keep"}.Contains(Mode)||duration<(Mode=="keep"?0:1)||duration>(Mode=="keep"?86400:180))return 2;
        IntPtr activation=IntPtr.Zero,controlSource=IntPtr.Zero,source=IntPtr.Zero,reader=IntPtr.Zero,type=IntPtr.Zero;
        bool initialized=false;int samples=0;var elapsed=Stopwatch.StartNew();
        try{
            Native.Check(Native.CoInitializeEx(IntPtr.Zero,0),"CoInitializeEx");
            Native.Check(Native.MFStartup(0x20070,0),"MFStartup");initialized=true;
            Log("start",new{duration,pnp=PnpState()});
            if(Mode=="keep"){KeepSourceOpen(duration);return 0;}
            while(elapsed.Elapsed.TotalSeconds<duration){
                activation=FindCamera();
                Log("poll",new{mfPresent=activation!=IntPtr.Zero,pnp=PnpState()});
                if(activation!=IntPtr.Zero&&Mode!="enum")break;
                Native.Release(ref activation);Thread.Sleep(1000);
            }
            if(activation==IntPtr.Zero||Mode=="enum")return 0;
            Native.Check(Native.Method<Native.Activate>(activation,33)(activation,ref MediaSource,out controlSource),"Activate control source");
            Native.Check(Native.Method<Native.Simple>(activation,35)(activation),"Detach control source");
            Log("control_source_open",new{pnp=PnpState()});
            if(Mode is "reader" or "stream"){
                Native.Check(Native.Method<Native.Activate>(activation,33)(activation,ref MediaSource,out source),"Activate reader source");
                Native.Check(Native.Method<Native.Simple>(activation,35)(activation),"Detach reader source");
                Native.Check(Native.MFCreateSourceReaderFromMediaSource(source,IntPtr.Zero,out reader),"MFCreateSourceReaderFromMediaSource");
                Native.Check(Native.Method<Native.GetNativeType>(reader,5)(reader,VideoStream,0,out type),"GetNativeMediaType");
                if(args.Length>2 && args[2]=="yuy2"){
                    var preferredSubtype=new Guid("32595559-0000-0010-8000-00aa00389b71");
                    for(uint index=0;index<256;index++){
                        IntPtr candidate=IntPtr.Zero;
                        var hr=Native.Method<Native.GetNativeType>(reader,5)(reader,VideoStream,index,out candidate);
                        if(hr<0)break;
                        var subtypeHr=Native.Method<Native.GetGuid>(candidate,10)(candidate,ref SubType,out var subtype);
                        var sizeHr=Native.Method<Native.GetU64>(candidate,8)(candidate,ref FrameSize,out var size);
                        if(subtypeHr>=0&&sizeHr>=0&&subtype==preferredSubtype&&(size>>32)==1280&&(size&0xffffffff)==720){
                            Native.Release(ref type);type=candidate;candidate=IntPtr.Zero;Log("preferred_type_selected",new{index});break;
                        }
                        Native.Release(ref candidate);
                    }
                }
                Log("native_type",FormatInfo(type));
                Native.Check(Native.Method<Native.SetMediaType>(reader,7)(reader,VideoStream,IntPtr.Zero,type),"SetCurrentMediaType");
                Native.Release(ref type);
            }
            var held=Stopwatch.StartNew();long lastReport=-1;
            while(held.Elapsed.TotalSeconds<duration){
                if(Mode=="stream"){
                    var hr=Native.Method<Native.ReadSample>(reader,9)(reader,VideoStream,0,out var actual,out var flags,out var timestamp,out var sample);
                    Native.Check(hr,"ReadSample");
                    try{if(sample!=IntPtr.Zero){samples++;if(samples==1||samples%30==0)Log("sample_statistics",new{samples,stats=SampleStats(sample)});}}
                    finally{Native.Release(ref sample);}
                    if((flags&3)!=0){Log("stream_flags",new{flags,actual,timestamp,samples});break;}
                }else Thread.Sleep(200);
                if(held.ElapsedMilliseconds/1000!=lastReport){lastReport=held.ElapsedMilliseconds/1000;Log("held",new{seconds=lastReport,samples,pnp=PnpState()});}
            }
            return 0;
        }catch(Exception e){Log("error",new{message=e.Message,hresult="0x"+e.HResult.ToString("X8"),samples,pnp=PnpState()});return 1;}
        finally{
            Log("cleanup_begin",new{samples,pnp=PnpState()});
            Native.Release(ref type);Native.Release(ref reader);
            Log("reader_released",new{pnp=PnpState()});
            if(source!=IntPtr.Zero)Native.Method<Native.Simple>(source,12)(source);
            Native.Release(ref source);
            Log("reader_source_shutdown",new{pnp=PnpState()});
            if(controlSource!=IntPtr.Zero)Native.Method<Native.Simple>(controlSource,12)(controlSource);
            Log("control_source_shutdown",new{pnp=PnpState()});
            Native.Release(ref controlSource);Native.Release(ref activation);
            Log("sources_released",new{pnp=PnpState()});
            if(initialized)Native.MFShutdown();Native.CoUninitialize();Log("end",new{samples,pnp=PnpState()});
        }
    }
}

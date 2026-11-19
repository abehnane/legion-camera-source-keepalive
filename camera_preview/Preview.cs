using System.Drawing.Imaging;
using System.Runtime.InteropServices;

static class CameraPreviewLauncher
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Program.ShowSoftwarePreview();
    }
}

static partial class Program
{
    public static void ShowSoftwarePreview()
    {
        using var form=new Form{Text="Integrated Camera — software preview, no recording",Width=1000,Height=650,StartPosition=FormStartPosition.CenterScreen};
        using var picture=new PictureBox{Dock=DockStyle.Fill,SizeMode=PictureBoxSizeMode.Zoom,BackColor=Color.Black};
        using var status=new Label{Dock=DockStyle.Bottom,Height=32,Text="Opening the physical camera through Microsoft Media Foundation..."};
        form.Controls.Add(picture);form.Controls.Add(status);
        using var cancellation=new CancellationTokenSource();
        int pending=0;
        form.FormClosing+=(_,_)=>cancellation.Cancel();
        form.Shown+=(_,_)=>{
            var thread=new Thread(()=>{
                IntPtr activation=IntPtr.Zero,source=IntPtr.Zero,reader=IntPtr.Zero,type=IntPtr.Zero;
                bool initialized=false;
                try{
                    Native.Check(Native.CoInitializeEx(IntPtr.Zero,0),"CoInitializeEx");
                    Native.Check(Native.MFStartup(0x20070,0),"MFStartup");initialized=true;
                    var until=DateTime.UtcNow.AddSeconds(30);
                    while(!cancellation.IsCancellationRequested&&DateTime.UtcNow<until){activation=FindCamera();if(activation!=IntPtr.Zero)break;Thread.Sleep(500);}
                    if(activation==IntPtr.Zero)throw new Exception("Physical camera is currently absent. Leave the camera helper running and try again.");
                    Native.Check(Native.Method<Native.Activate>(activation,33)(activation,ref MediaSource,out source),"Activate physical camera");
                    Native.Check(Native.Method<Native.Simple>(activation,35)(activation),"Detach physical source");
                    Native.Check(Native.MFCreateSourceReaderFromMediaSource(source,IntPtr.Zero,out reader),"Create source reader");
                    uint width=0,height=0;
                    var yuy2=new Guid("32595559-0000-0010-8000-00aa00389b71");
                    for(uint i=0;i<256;i++){
                        IntPtr candidate=IntPtr.Zero;
                        var hr=Native.Method<Native.GetNativeType>(reader,5)(reader,VideoStream,i,out candidate);
                        if(hr<0)break;
                        Native.Method<Native.GetGuid>(candidate,10)(candidate,ref SubType,out var format);
                        Native.Method<Native.GetU64>(candidate,8)(candidate,ref FrameSize,out var size);
                        if(format==yuy2&&(size>>32)==1280&&(size&0xffffffff)==720){type=candidate;width=(uint)(size>>32);height=(uint)size;break;}
                        Native.Release(ref candidate);
                    }
                    if(type==IntPtr.Zero)throw new Exception("1280×720 YUY2 is unavailable; this preview intentionally avoids a GPU conversion path.");
                    Native.Check(Native.Method<Native.SetMediaType>(reader,7)(reader,VideoStream,IntPtr.Zero,type),"Select YUY2");
                    int frames=0;
                    while(!cancellation.IsCancellationRequested){
                        Native.Check(Native.Method<Native.ReadSample>(reader,9)(reader,VideoStream,0,out _,out var flags,out _,out var sample),"Read frame");
                        try{
                            if(sample==IntPtr.Zero)continue;
                            frames++;
                            if(Interlocked.CompareExchange(ref pending,1,0)!=0)continue;
                            Bitmap? bitmap=null;
                            try{
                                bitmap=DecodeYuy2Sample(sample,(int)width,(int)height);
                                var next=bitmap;var frameNumber=frames;
                                form.BeginInvoke(()=>{
                                    var old=picture.Image;picture.Image=next;old?.Dispose();
                                    status.Text=$"Physical integrated camera | {width}×{height} YUY2 | frame {frameNumber} | CPU conversion | no recording";
                                    Interlocked.Exchange(ref pending,0);
                                });
                                bitmap=null;
                            }finally{bitmap?.Dispose();}
                        }finally{Native.Release(ref sample);}
                        if((flags&3)!=0)break;
                    }
                }catch(Exception e){
                    if(!cancellation.IsCancellationRequested&&!form.IsDisposed){try{form.BeginInvoke(()=>status.Text=$"Camera error: {e.Message} (0x{e.HResult:X8})");}catch(InvalidOperationException){}}
                }finally{
                    Native.Release(ref type);Native.Release(ref reader);
                    if(source!=IntPtr.Zero)Native.Method<Native.Simple>(source,12)(source);
                    Native.Release(ref source);Native.Release(ref activation);
                    if(initialized)Native.MFShutdown();Native.CoUninitialize();
                }
            }){IsBackground=true,Name="Physical camera software preview"};
            thread.SetApartmentState(ApartmentState.MTA);thread.Start();
        };
        Application.Run(form);
        picture.Image?.Dispose();
    }

    static Bitmap DecodeYuy2Sample(IntPtr sample,int width,int height)
    {
        IntPtr buffer=IntPtr.Zero;
        Native.Check(Native.Method<Native.GetBuffer>(sample,41)(sample,out buffer),"Get video buffer");
        byte[] yuy2;
        try{
            Native.Check(Native.Method<Native.LockBuffer>(buffer,3)(buffer,out var bytes,out _,out var length),"Lock video buffer");
            try{if(length<(uint)(width*height*2))throw new Exception("Short YUY2 frame.");yuy2=new byte[width*height*2];Marshal.Copy(bytes,yuy2,0,yuy2.Length);}
            finally{Native.Method<Native.Simple>(buffer,4)(buffer);}
        }finally{Native.Release(ref buffer);}
        var pixels=new byte[width*height*4];
        static byte Clip(int v)=>(byte)Math.Clamp(v,0,255);
        for(int i=0,j=0;i<yuy2.Length;i+=4,j+=8){
            int u=yuy2[i+1]-128,v=yuy2[i+3]-128;
            for(int k=0;k<2;k++){
                int y=Math.Max(0,yuy2[i+2*k]-16),p=j+4*k;
                pixels[p]=Clip((298*y+516*u+128)>>8);
                pixels[p+1]=Clip((298*y-100*u-208*v+128)>>8);
                pixels[p+2]=Clip((298*y+409*v+128)>>8);pixels[p+3]=255;
            }
        }
        var bitmap=new Bitmap(width,height,PixelFormat.Format32bppArgb);
        var locked=bitmap.LockBits(new Rectangle(0,0,width,height),ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);
        try{Marshal.Copy(pixels,0,locked.Scan0,pixels.Length);}finally{bitmap.UnlockBits(locked);}
        return bitmap;
    }
}

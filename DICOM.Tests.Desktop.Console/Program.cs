using Dicom;
using Dicom.Imaging;
using Dicom.Log;
using Dicom.Network;
using System;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DICOM.Tests.Desktop.Console
{
    class Program
    {


        static void Main(string[] args)
        {
            //Send_MultipleRequests_AllRecognized(5);
            TestImageRendering();
        }


        public static void TestImageRendering()
        {
            var dcm = new DicomImage(DicomFile.Open("EXP0000.dcm").Dataset);
            //Gray Scale correction
            dcm.WindowCenter = 128;
            dcm.ShowOverlays = false;

            // All frames to stack
            for (int i = 0; i < dcm.NumberOfFrames; i++)
            {
                Bitmap bmpSlice;
                Bitmap bmpSlice2;
                try
                {
                    bmpSlice = dcm.RenderImage(i).As<Bitmap>();
                    // do some little things
//                    Thread.Sleep(TimeSpan.FromSeconds(1));
                 //   System.Console.WriteLine($"Bitmap number {i} rendered in {bmpSlice.Width}x{bmpSlice.Height}");
                    bmpSlice2 = new Bitmap(bmpSlice);
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine(ex.ToString());
                }
            }
        }

        /*
        public static void Send_MultipleRequests_AllRecognized(int expected)
        {
            var port = 11113;
            var flag = new ManualResetEventSlim();

            using (DicomServer.Create<MyDicomCEchoProvider>(port))
            {
                var actual = 0;
                DicomCEchoRequest.ResponseDelegate callback = (req, res) =>
                {
                    System.Console.WriteLine("++ Received response");
                    Interlocked.Increment(ref actual);
                    if (actual == expected) flag.Set();
                };

                var client = new DicomClient();
                client.NegotiateAsyncOps(expected, 1);

                for (var i = 0; i < expected; ++i)
                    client.AddRequest(new DicomCEchoRequest { OnResponseReceived = callback });

                client.Send("127.0.0.1", port, false, "SCU", "ANY-SCP");
                flag.Wait(10000);
            }

            System.Console.ReadLine();
        }


    }

  
    public class MyDicomCEchoProvider : DicomService, IDicomServiceProvider, IDicomCEchoProvider
    {

        public MyDicomCEchoProvider(INetworkStream stream, Encoding fallbackEncoding, Logger log)
            : base(stream, fallbackEncoding, log)
        {
            System.Console.WriteLine("MyDicomCEchoProvider starting initialization");
            // very long lasting initialization
            System.Threading.Thread.Sleep(TimeSpan.FromSeconds(2));
            System.Console.WriteLine("MyDicomCEchoProvider initialization finished");
        }

        public void OnReceiveAbort(DicomAbortSource source, DicomAbortReason reason)
        {
        }

        public void OnConnectionClosed(Exception exception)
        {
        }

        public DicomCEchoResponse OnCEchoRequest(DicomCEchoRequest request)
        {
            System.Console.WriteLine("-- Received CEchoRequest ");
            Thread.Sleep(TimeSpan.FromMilliseconds(1000));
            return new DicomCEchoResponse(request, DicomStatus.Success);
        }

        public Task OnReceiveAssociationRequestAsync(DicomAssociation association)
        {
            System.Console.WriteLine("... Association Request");
            foreach (var pc in association.PresentationContexts)
            {
                pc.SetResult(pc.AbstractSyntax == DicomUID.Verification
                    ? DicomPresentationContextResult.Accept
                    : DicomPresentationContextResult.RejectAbstractSyntaxNotSupported);
            }

            return SendAssociationAcceptAsync(association);
        }

        public Task OnReceiveAssociationReleaseRequestAsync()
        {
            return SendAssociationReleaseResponseAsync();
        }

    */
    }
    
}

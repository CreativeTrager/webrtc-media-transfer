using System;
using System.Net;
using System.Text;
using System.Threading;
using SIPSorcery.Net;
using CreativeTrager.WebRtcMediaSender.WebRtc;
using WebSocketSharp.Server;


var webSocketServer = new WebSocketServer(IPAddress.Any, port: 8081);
webSocketServer.AddWebSocketService<WebRTCWebSocketPeer>(path: "/",
	initializer: peer => peer.CreatePeerConnection = () =>
		WebRtcPeerConnection.Create(webcamId: "USB Video Device")
);

Console.WriteLine(
	new StringBuilder()
		.Append("Starting web socket server...")
		.ToString()
);
webSocketServer.Start();

Console.WriteLine(
	new StringBuilder()
		.Append($"Waiting for web socket connections on ")
		.Append($"{webSocketServer.Address}:{webSocketServer.Port}...").AppendLine()
		.Append($"Press Ctrl-C to exit.")
		.ToString()
);

var exitEvent = new ManualResetEvent(initialState: false);
Console.CancelKeyPress += (_, e) => { e.Cancel = true; exitEvent.Set(); };
exitEvent.WaitOne();
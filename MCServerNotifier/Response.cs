using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace MCServerNotifier
{
    public class Response
    {
        public static byte[] ParseHandshake(byte[] data)
        {
            var response = BitConverter.GetBytes(int.Parse(Encoding.ASCII.GetString(data, 5, data.Length - 6)));
            if (BitConverter.IsLittleEndian)
            {
                response = response.Reverse().ToArray();
            }
            
            return response;
        }

        public static ServerFullState ParseFullState(byte[] data)
        {
			data = data.Skip(16).ToArray();

			string stringData = Encoding.ASCII.GetString(data);

			//This array should contain an array with server informations and an array with playernames
			string[] informations = stringData.Split(new[] {"player_\0\0"}, StringSplitOptions.None);

			string[] serverInfoArr = informations[0].Split(new[] { "\0" }, StringSplitOptions.None);
			string[] playerList = informations[1].Split(new[] { "\0" }, StringSplitOptions.None)
				.Where(s => !string.IsNullOrEmpty(s)).ToArray();

			//Split serverInfo to key - value pair.

			Dictionary<string, string> serverDict = new Dictionary<string, string>();

			for (int i = 0; i < serverInfoArr.Length; i += 2)
			{
				serverDict.Add(serverInfoArr[i], serverInfoArr[i + 1]);
			}

			//0 = MOTD
			//1 = GameType
			//2 = Map
			//3 = Number of Players
			//4 = Maxnumber of Players
			//5 = Host Port
			//6 = Host IP

			ServerFullState fullState = new ServerFullState
			{
				Motd = serverDict["hostname"],
				GameType = serverDict["gametype"],
				Map = serverDict["map"],
				PlayerCount = int.Parse(serverDict["numplayers"]),
				MaxPlayers = int.Parse(serverDict["maxplayers"]),
				PlayerList =  playerList,
				Plugins = serverDict["plugins"],
				Address = serverDict["hostip"],
				Port = int.Parse(serverDict["hostport"]),
				Version = serverDict["version"]
			};

            return fullState;
        }
    }
}
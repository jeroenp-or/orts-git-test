// COPYRIGHT 2012, 2013 by the Open Rails project.
// 
// This file is part of Open Rails.
// 
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Open Rails is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Open Rails.  If not, see <http://www.gnu.org/licenses/>.

// This file is the responsibility of the 3D & Environment Team. 

using Orts.Formats.Msts;
using ORTS.Common;
using System.Collections.Generic;
using System.Linq;

namespace Orts.Simulation
{
    public class HazardManager
	{
		readonly int hornDist = 200;
		readonly int approachDist = 160;
        readonly int scaredDist = 147;
		readonly Simulator Simulator;
		public readonly Dictionary<int, Hazard> Hazards;
		public readonly Dictionary<int, Hazard> CurrentHazards;
		public readonly Dictionary<string, HazardFile> HazFiles;
		List<int> InterestedHazards;//those hazards is closed to player, needs to listen to horn
		public HazardManager(Simulator simulator)
		{
			Simulator = simulator;
			InterestedHazards = new List<int>();
			CurrentHazards = new Dictionary<int, Hazard>();
			HazFiles = new Dictionary<string, HazardFile>();
			Hazards = simulator.TDB != null && simulator.TDB.TrackDB != null ? GetHazardsFromDB(simulator.TDB.TrackDB.TrackNodes, simulator.TDB.TrackDB.TrItemTable) : new Dictionary<int, Hazard>();
		}

		static Dictionary<int, Hazard> GetHazardsFromDB(TrackNode[] trackNodes, TrItem[] trItemTable)
		{
			return (from trackNode in trackNodes
					where trackNode != null && trackNode.TrVectorNode != null && trackNode.TrVectorNode.NoItemRefs > 0
					from itemRef in trackNode.TrVectorNode.TrItemRefs.Distinct()
					where trItemTable[itemRef] != null && trItemTable[itemRef].ItemType == TrItem.trItemType.trHAZARD
					select new KeyValuePair<int, Hazard>(itemRef, new Hazard(trackNode, trItemTable[itemRef])))
					.ToDictionary(_ => _.Key, _ => _.Value);
		}

		[CallOnThread("Loader")]
		public Hazard AddHazardIntoGame(int itemID, string hazFileName)
		{
			try
			{
				if (!CurrentHazards.ContainsKey(itemID))
				{
					if (HazFiles.ContainsKey(hazFileName)) Hazards[itemID].HazFile = HazFiles[hazFileName];
					else
					{
						var hazF = new HazardFile(Simulator.RoutePath + "\\" + hazFileName);
						HazFiles.Add(hazFileName, hazF);
						Hazards[itemID].HazFile = hazF;
					}
					//based on act setting for frequency
                    if (Hazards[itemID].animal == true && Simulator.Activity != null)
                    {
                        if (Simulator.Random.Next(100) > Simulator.Activity.Tr_Activity.Tr_Activity_Header.Animals) return null;
                    }
					else if (Simulator.Activity != null)
					{
						if (Simulator.Random.Next(100) > Simulator.Activity.Tr_Activity.Tr_Activity_Header.Animals) return null;
					}
					else //in explore mode
					{
						if (Hazards[itemID].animal == false) return null;//not show worker in explore mode
						if (Simulator.Random.Next(100) > 20) return null;//show 10% animals
					}
					CurrentHazards.Add(itemID, Hazards[itemID]);
					return Hazards[itemID];//successfully added the hazard with associated haz file
				}
			}
			catch { }
			return null;
		}

		public void RemoveHazardFromGame(int itemID)
		{
			try
			{
				if (CurrentHazards.ContainsKey(itemID))
				{
					CurrentHazards.Remove(itemID);
				}
			}
			catch { };
		}

		[CallOnThread("Updater")]
		public void Update(float elapsedClockSeconds)
		{
			var playerLocation = Simulator.PlayerLocomotive.WorldPosition.WorldLocation;

			foreach (var haz in Hazards)
			{
				haz.Value.Update(playerLocation, approachDist, scaredDist);
			}
		}

		public void Horn()
		{
			var playerLocation = Simulator.PlayerLocomotive.WorldPosition.WorldLocation;
			foreach (var haz in Hazards)
			{
				if (WorldLocation.Within(haz.Value.Location, playerLocation, hornDist))
				{
					haz.Value.state = Hazard.State.LookLeft;
				}
			}
		}
	}

	public class Hazard
	{
        readonly TrackNode TrackNode;

        internal WorldLocation Location;
		public HazardFile HazFile { get { return hazF; } set { hazF = value; if (hazF.Tr_HazardFile.Workers != null) animal = false; else animal = true; } }
		public HazardFile hazF;
		public enum State { Idle1, Idle2, LookLeft, LookRight, Scared };
		public State state;
		public bool animal = true;

		public Hazard(TrackNode trackNode, TrItem trItem)
        {
            TrackNode = trackNode;
            Location = new WorldLocation(trItem.TileX, trItem.TileZ, trItem.X, trItem.Y, trItem.Z);
			state = State.Idle1;
        }

		public void Update(WorldLocation playerLocation, int approachDist, int scaredDist)
		{
			if (state == State.Idle1)
			{
				if (Simulator.Random.Next(10) == 0) state = State.Idle2;
			}
			else if (state == State.Idle2)
			{
				if (Simulator.Random.Next(5) == 0) state = State.Idle1;
			}

            if (!WorldLocation.Within(Location, playerLocation, scaredDist) && state < State.LookLeft)
            {
                if (WorldLocation.Within(Location, playerLocation, approachDist) && state < State.LookLeft)
                {
                    state = State.LookRight;
                }
            }
            if (WorldLocation.Within(Location, playerLocation, scaredDist) && state == State.LookRight || state == State.LookLeft)
            {
                state = State.Scared;
            }
       }
	}
}

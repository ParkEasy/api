namespace ParkEasyAPI.Models
{	
    public class ParkingModel 
	{
		public string Id;
		public string Name;	
		public string Description;
		public ParkingType Type;
		public CoordinateModel Coordinate;
		public double[] Coordinates;
		public int? Capacity; // free spaces in parking space
		public int? CapacityWomen; // free spaces especially for women
		public int? CapacityDisabled; // free spaces for disabled humans
		public int? CapacityService; // free spaces for Dienstfahrzeuge 
		public int Free;
		public double FreeLikelihood;
		public PriceModel Price;
		public double? MaximumParkingHours;
		public string RedPointText; // some payment stuff
		public string SectionFrom;
		public string SectionTo;
		public double DistanceToUser;
		public bool Gates;
		public bool ReceivedVotes;
		public OpeningHoursModel[] OpeningHours;
		
		public  ParkingModel() {
			this.Gates = false;
			this.Coordinates = new double[2];
			this.FreeLikelihood = 0.0;
			this.Free = -1;
			this.ReceivedVotes = false;
		}
		
		public void CalcFreeLikelihood()
		{
			// no information yet? set the likelihood of a free parking space to 50%
			if(this.Free < 0 && this.ReceivedVotes == false)
            {
                this.FreeLikelihood = 0.5;
            }
            
            // if there are information on the current status of free parking spots,
            // set the likelihood to the amount of free spots devided by the capacity
            else
            {
                if(this.Type == ParkingType.Garage) 
                {
                    if(this.Free == 0) this.FreeLikelihood = 0.0;
                    else if(this.Free == 1) this.FreeLikelihood = 0.05;
                    else if(this.Free == 2) this.FreeLikelihood = 0.10;
                    else if(this.Free == 3) this.FreeLikelihood = 0.15;
              
                    else this.FreeLikelihood = 1.0;
                }
            }
		}
	}
	
}
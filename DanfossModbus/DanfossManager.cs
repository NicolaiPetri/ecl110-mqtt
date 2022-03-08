using FluentModbus;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;


// REF: https://github.com/Ingramz/ecl110
// REF: https://assets.danfoss.com/documents/66748/AQ188586469712en-010701.pdf

namespace DanfossModbus
{
    public class DanfossManager
    {
        static int danfossAddr = 5;
        ModbusRtuClient client;
        private Dictionary<int, short> LastStateMap = new ();
        public DanfossManager()
        {
            // use default COM port settings
//            var client = new ModbusRtuClient();

            // use custom COM port settings:
            client = new ModbusRtuClient()
            {
                BaudRate = 19200,
                Parity = Parity.Even,
                StopBits = StopBits.One
                
            };

            client.Connect("/dev/ttyUSB2", ModbusEndianness.BigEndian);
            DoRead();
        }

        private short[] ReadSeries(int start, int count, bool verbose=true) {
            if (verbose)
                Console.WriteLine($"Fetching {count} registers from {start} address");
            var dataSeries = client.ReadHoldingRegisters<short>(danfossAddr, start, count);
                for (var i =0; i<count; i++)
                {
                    var d = dataSeries[i];
                    var key = start+i;
                    if (LastStateMap.ContainsKey(key)) {
                        var curVal = LastStateMap[key];
                        if (curVal != d) {
                            Console.WriteLine($"{DateTime.UtcNow} CHANGE DETECTED FOR {key}: {curVal} ==> {d}");
                        }
                        LastStateMap[key] = d;
                    } else {
                        LastStateMap.Add(key, d);
                    }
                    if (verbose) {
                        Console.WriteLine($" - {key} read {d}");
                    }
                }
            return dataSeries.ToArray();
        }
        private bool firstRun = true;
        public DanfossECLState ReadState() {

            if (false) {
                for (var reg = 0; reg < 65000; reg++) {
                    try {
                        if (firstRun || LastStateMap.ContainsKey(reg)) { 
                            var rVal = ReadSeries(reg, 1, false);
                            Console.WriteLine($"INV: Found value in reg {reg}: {rVal[0]}");
                        }
                    } catch (Exception e) {
                    }
                }
            }
            firstRun = false;

            var mondaySchedule =   ReadSeries(1109, 4);
            var tuesdaySchedule =  ReadSeries(1119, 4);
            var wednesdaySchedule= ReadSeries(1129, 4);
            var thursdaySchedule = ReadSeries(1139, 4);
            var fridaySchedule =   ReadSeries(1149, 4);
            var saturdaySchedule=  ReadSeries(1159, 4);
            var sundaySchedule =   ReadSeries(1169, 4);
            var ntest8 =           ReadSeries(2002, 1);
            var ntest9 =           ReadSeries(2007, 1);
            var ntest10 =          ReadSeries(2010, 1);
            var ntest11 =          ReadSeries(2014, 1);
            var ntest12 =          ReadSeries(2027, 1);
            var footest2 =         ReadSeries(2102, 9);
            var pumpOnOff =        ReadSeries(4001, 1, false);
            var valveStatus =      ReadSeries(4100, 2, false);
            var desiredMode =      ReadSeries(4200, 1, false);
            var actualMode =       ReadSeries(4210, 1, false);
            var footest1 =         ReadSeries(4614, 1);
            var test =             ReadSeries(11009, 6);
            var stopAndExercise =  ReadSeries(11019, 5, false);
            var ntest13 =          ReadSeries(11029, 1);
            var ntest14 =          ReadSeries(11034, 3);
            var ntest15 =          ReadSeries(11051, 1);
            var test3 =            ReadSeries(11076, 2);
            var ntest16 =          ReadSeries(11084, 1);
            var ntest17 =          ReadSeries(11092, 1);
            var test4 =            ReadSeries(11099, 1);
            var ntest18 =          ReadSeries(11140, 1);
            var ntest19 =          ReadSeries(11161, 1);
            var test5 =            ReadSeries(11173, 14);
            var test6 =            ReadSeries(11188, 1);
            var ntest20 =          ReadSeries(11197, 2);
            var tempSensors =      ReadSeries(11200, 4, false);
            // 11220 x 4 = 1920 (No available)
            var desiredTemp =      ReadSeries(11228, 2, false);
            var test7 =            ReadSeries(60007, 1);
            var test8 =            ReadSeries(60020, 1);
            var test9 =            ReadSeries(60025, 1);
            var ntest21 =          ReadSeries(60057, 2);
            var time =             ReadSeries(64044, 5);

            var state = new DanfossECLState { 
                PumpActive = pumpOnOff[0] > 0,
                TemperatureS1 = ((decimal)tempSensors[0])/10, 
                TemperatureS2 = tempSensors[1] != 1920 ? ((decimal)tempSensors[1])/10 : null, 
                TemperatureS3 = ((decimal)tempSensors[2])/10, 
                TemperatureS4 = ((decimal)tempSensors[3])/10,

                TemperatureS2Desired = ((decimal)desiredTemp[0])/10,
                TemperatureS3Desired = ((decimal)desiredTemp[1])/10,

		TemperatureSet = ((decimal)test5[6]),

                TotalStop = stopAndExercise[1] > 0,
                PumpExerciseEnabled = stopAndExercise[2] > 0,
                ValveExerciseEnabled = stopAndExercise[3] > 0,
                ActuatorType = stopAndExercise[4] == 0 ? "ABV" : "GEAR",

                // Experimental
                ValveOpen = valveStatus[0] > 0,
                ValveShut = valveStatus[1] > 0,

                SelectedMode = MapSelectedMode(desiredMode[0]),
                ActualMode = MapActiveMode(actualMode[0]),

		DateTime = $"{(2000+time[2]):0000}-{time[3]:00}-{time[4]:00} {time[0]:00}:{time[1]:00}"
                
            };
            return state;
        }
	public string MapSelectedMode(int num) {
		if (num == 1) return "Auto";
		if (num == 2) return "Comfort";
		if (num == 3) return "Reduce";
		if (num == 4) return "Standby";
		return $"Unknown: {num}";
	}
	public string MapActiveMode(int num) {
		if (num == 0) return "Setback";
		//if (num == 1) return "Auto";
		if (num == 2) return "Comfort";
		//if (num == 3) return "Reduce";
		// if (num == 4) return "Standby";
		return $"Unknown: {num}";
	}
        public void DoRead()
        {
            Console.WriteLine("Dumping data:");
            var unitIdentifier = danfossAddr; // 0x00 and 0xFF are the defaults for TCP/IP-only Modbus devices.
            var startingAddress = 11200;
            var count = 4;
            var dataSeries = client.ReadHoldingRegisters<short>(unitIdentifier, startingAddress, count);
            var state = new DanfossECLState { 
                TemperatureS1 = ((decimal)dataSeries[0])/10 
            };
            Console.WriteLine($"Got temp S1: {state.TemperatureS1}");

            foreach (var d in dataSeries)
            {
                Console.WriteLine($"Value is {d}");

            }
            //            var firstValue = floatData[0];
            //           var lastValue = floatData[floatData.Length - 1];

            //          Console.WriteLine($"Fist value is {firstValue}");
        }
    }

    public class DanfossECLState {

        public Decimal? TemperatureS1 {get; set ;}
        public Decimal? TemperatureS2 {get; set ;}
        public Decimal? TemperatureS3 {get; set ;}
        public Decimal? TemperatureS4 {get; set ;}

        public Decimal? TemperatureS2Desired {get; set;}
        public Decimal? TemperatureS3Desired {get; set;}
        
	public Decimal? TemperatureSet {get; set;}

        public Boolean? PumpActive {get; set;}
        public Boolean? TotalStop {get; set;}
        public Boolean? PumpExerciseEnabled {get; set;}
        public Boolean? ValveExerciseEnabled {get; set;}
        public string ActuatorType {get; set;}

        // Experimental
        public Boolean? ValveOpen {get; set;}
        public Boolean? ValveShut {get; set;}

        public string SelectedMode {get; set;}
        public string ActualMode {get; set;}

	public string DateTime {get; set; }
    }
}


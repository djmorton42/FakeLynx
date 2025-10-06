using FakeLynx;

// Test the lap logic
var testLaps = new[] { 4.5, 9.0, 13.5 };

foreach (var laps in testLaps)
{
    var race = new Race(laps);
    Console.WriteLine($"Laps: {laps}");
    Console.WriteLine($"  HasHalfLap: {race.HasHalfLap}");
    Console.WriteLine($"  HasWholeLaps: {race.HasWholeLaps}");
    Console.WriteLine($"  Should send start crossings: {race.HasWholeLaps}");
    Console.WriteLine();
}

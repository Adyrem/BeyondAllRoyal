// Implemented by buildings that passively inject energy into nearby friendly
// buildings within a fixed range (HQ, TeslaTower — see Building.InjectEnergyIntoNearby).
// Lets range-highlighting UI (BuildingPlacer's coverage overlay) find every
// currently-placed injector generically, without hard-coding each type.
public interface IEnergyInjector
{
    float InjectionRange { get; }
}

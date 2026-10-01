namespace EventService.Domain;

public sealed class Zone
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public decimal Price { get; private set; }
    public int Capacity { get; private set; }

    private Zone(Guid id, string name, decimal price, int capacity)
    {
        Id = id;
        Name = name;
        Price = price;
        Capacity = capacity;
    }

    public static Zone Create(string name, decimal price, int capacity)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("El nombre de la zona es obligatorio.");
        if (price < 0)
            throw new DomainException("El precio de una zona no puede ser negativo.");
        if (capacity <= 0)
            throw new DomainException("La capacidad de una zona debe ser mayor a cero.");

        return new Zone(Guid.NewGuid(), name.Trim(), price, capacity);
    }
}

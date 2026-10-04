using System.ComponentModel;
using System.Runtime.CompilerServices;
using RobotPlanet.Core.Properties;

namespace RobotPlanet.Core
{
    public abstract class Robot : INotifyPropertyChanged
    {
        public const int MaxLevel = 100;

        private int _battery;
        private int _integrity;

        public event PropertyChangedEventHandler? PropertyChanged;

        public int Id { get; }
        public string Name { get; }
        public string Kind => GetType().Name;

        public int Battery
        {
            get => _battery;
            private set
            {
                if (_battery == value) return;
                _battery = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsOperational));
            }
        }

        public int Integrity
        {
            get => _integrity;
            private set
            {
                if (_integrity == value) return;
                _integrity = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsOperational));
            }
        }

        public bool IsOperational => Battery > 0 && Integrity > 0;

        protected Robot(int id, string name, int battery = MaxLevel, int integrity = MaxLevel)
        {
            if (id < 0 || id > 999999)
                throw new ArgumentException(Resources.Err_Invalid_id, nameof(id));
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException(Resources.Err_Invalid_name, nameof(name));

            name = name.Trim();
            if (name.Length < 2 || name.Length > 20)
                throw new ArgumentException(Resources.Err_Invalid_name, nameof(name));

            if (battery < 0 || battery > MaxLevel)
                throw new ArgumentException(Resources.Err_Invalid_battery, nameof(battery));
            if (integrity < 0 || integrity > MaxLevel)
                throw new ArgumentException(Resources.Err_Invalid_integrity, nameof(integrity));

            Id = id;
            Name = name;
            Battery = battery;
            Integrity = integrity;
        }

        // Tavaline tegevus: virtual
        public virtual string DoWork() => string.Format(Resources.Robot_DoWork, Name);

        // Hullumeelne tegevus: igal tüübil oma
        public abstract string CrazyAction();

        // Avalik remondi sisenemispunkt (kutsub RepairBot)
        public string ReceiveRepair(int amount)
        {
            if (amount <= 0 || amount > MaxLevel)
                return string.Format(Resources.Robot_Repair_InvalidAmount, Name, amount);
            if (Integrity == MaxLevel)
                return string.Format(Resources.Robot_Repair_AlreadyPerfect, Name);

            int before = Integrity;
            TryRestoreIntegrity(amount);
            return string.Format(Resources.Robot_Repair_Done, Name, before, Integrity);
        }

        // ---- Kaitstud oleku muutmine (vigane sisend ei muuda olekut) ----

        protected bool TrySpendBattery(int amount)
        {
            if (amount < 0 || amount > Battery) return false;
            Battery -= amount;
            return true;
        }

        protected bool TryAddBattery(int amount)
        {
            if (amount < 0) return false;
            Battery = Math.Min(MaxLevel, Battery + amount);
            return true;
        }

        protected bool TryDamage(int amount)
        {
            if (amount < 0) return false;
            Integrity = Math.Max(0, Integrity - amount);
            return true;
        }

        protected bool TryRestoreIntegrity(int amount)
        {
            if (amount < 0) return false;
            Integrity = Math.Min(MaxLevel, Integrity + amount);
            return true;
        }

        public override string ToString() =>
            string.Format(Resources.Robot_ToString, Name, Id, GetType().Name);

        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
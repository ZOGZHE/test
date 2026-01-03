using System.Collections.Generic;
using UnityEngine;

namespace ConnectMaster
{
    #region 物品分类枚举
    [System.Serializable]
    public enum ItemCategory
    {
        //1
        Floor,
        Wall,
        ThrowPillow,
        Electronics,

        Pets,
        Cabinet,
        Plant,
        Beverage,

        Avatar,
        Weather,
        File,
        Clock,

        Package,
        Chair,
        Lamp,
        Knowledge,

        //2
        Knives,
        Garbage,
        Clean,
        Wind,

        Fastfood,
        Hotair,
        Appliances,
        Cookware,

        FrozenMeat,
        Box,
        Plate,
        Ingredients,

        Fabric,
        Jar,
        Shovel,
        EdibleSauce,
        Tableware,

        //3
        Tools,
        Japan,
        Wood,
        Footwear,

        Circle,
        Lantern,
        AntiTheft,
        CoolDown,

        Seafood,
        FriedFood,
        Bagged,
        Bottles,

        SingleChair,
        KitchenUtensils,
        Gardening,
        Insect,

        //4
        Nuts,
        Warehouse,
        Fruit,
        Birds,

        Scale,
        GreenTravel,
        Honey,
        Bank,

        Organ,
        Purple,
        Green,
        Strawberry,
        Collar,
        Yellow,

        Teeth,
        Ladder,
        FruitPie,
        Pens,
        SportsEvents,

        //5
        Tool,
        Project,
        Fire,
        CannedFood,

        Cake,
        Valuables,
        Transportation,
        Alcohol,

        Coffee,
        Baking,
        Doll,
        Carbohydrate,
        ThinBiscuit,

        PlasticBottle,
        Signpost,
        Essentials,
        Egg,
        SpicyFood,

        //6
        Smell,
        Diving,
        Electricity,
        Rope,

        MowingGrass,
        SkinCare,
        Plug,
        Clothes,
        MagicShow,

        IceCream,
        Drumming,
        Amplification,
        Eye,
        Net,
        DanceParty,
        Speaker,

        Helmet,
        Hook,
        ScenicSpot,
        Leaves,
        Chessboard,
        Studio,
        BackCushion,

        //7
        Outerspace,
        Ball,
        Planet,
        Ringtone,

        Research,
        Hat,
        Virus,
        Hairdressing,

        Fitness,
        Cup,
        Sweep,
        Yoga,
        Toilet,

        Slippers,
        Nutlet,
        Paper,
        Cosmetics,
        Expression,

        Vehicle,
        Satellite,
        Towel,
        Lighting,
        Candle,

        //8
        IceSports,
        Travel,
        Sacred,
        Running,

        Umbrella,
        Christmas,
        Antibiotic,
        Vegetables,

        Section,
        Bouquet,
        Wild,
        Equestrian,

        Hallowmas,
        Equipment,
        PetFood,
        Mining,

        Light,
        Wrap,
        Jean,
        Animals,

        Poultry,
        Reptiles,
        GetMarried,
        Fertility,

        //9
        FiveSenses,
        Factory,
        Construction,
        Temperature,
        Golf,

        Notarization,
        AncientWars,
        Beach,
        Gamble,
        Ceramic,

        Soldier,
        Missile,
        SteeringWheel,
        Calculate,
        LongNeck,

        KitchenWare,
        KitchenAppliances,
        Billiards,
        Pizza,
        Juicer,

        InstrumentPanel,
        WoodenBarrel,
        DeepSea,
        WesternCowboy,
        Supermarket,

        BandComponents,
        Instrument,
        Gloves,
        Film,
        Festival,
        Ruler,

        None,
    }

    #endregion

    #region 物品数据模型
    [System.Serializable]
    public class Item
    {
        public int id;             // 唯一ID（与ItemDatabase中ID对应）
        public string name;        // 物品名称
        public Sprite itemIcon;    // 2D UI图标
        public bool isSupplement=false;
        public ItemCategory category; // 所属类别
    }
    #endregion

    #region UI物品数据载体（挂载到Item UI上）
    public class ItemComponent : MonoBehaviour
    {
        public Item item; // 关联的物品核心数据
    }
    #endregion
}
using System.Globalization;

namespace DietTracker.Core;

/// <param name="PieceGrams">Typical weight of one piece (one onion, one egg, one clove); null when counting makes no sense.</param>
/// <param name="Seasoning">Used in tiny amounts: a missing quantity counts as zero instead of "unknown".</param>
/// <param name="CupGrams">Weight of one cup for dense dry foods (flour, sugar, rice); null means "a cup of water" (240 g).</param>
/// <param name="FryingOil">Cooking oil: large amounts are for deep frying and mostly stay in the pan.</param>
public record FoodEntry(
    string Name, double Kcal, double Protein, double Carbs, double Fat,
    double? PieceGrams, bool Seasoning, double? CupGrams, bool FryingOil);

/// <summary>
/// Approximate nutrition per 100 g for common ingredients (English and Croatian names), typical raw/dry values.
/// Estimates only: meant to be right within roughly 10-20% for a home-cooked meal.
/// </summary>
public static class FoodTable
{
    // aliases ; separated | kcal | protein | carbs | fat | piece grams | flags ("s" seasoning, "o" frying oil) | cup grams
    // Trailing optional columns may be left empty.
    private const string Raw = """
        chicken breast;chicken fillet;pileca prsa;pileci file;pileci medaljoni | 120 | 22.5 | 0 | 2.6 | 170
        chicken thigh;chicken leg;chicken drumstick;chicken wing;pileci batak;batak | 177 | 18 | 0 | 11 | 120
        chicken;piletina;pileci | 150 | 15 | 0 | 9.5
        turkey;turkey breast;puretina;curetina | 135 | 21 | 0 | 5
        minced beef;beef mince;ground beef;mljevena junetina;mljeveno meso;mince | 250 | 17 | 0 | 20
        beef;steak;beef steak;junetina;govedina;biftek | 190 | 21 | 0 | 11
        pork;pork chop;pork loin;pork belly;svinjetina;kotlet | 200 | 19 | 0 | 14
        bacon;pancetta;slanina | 540 | 37 | 1.4 | 42 | 15
        ham;sunka | 145 | 21 | 1.5 | 6
        sausage;kobasica | 300 | 12 | 2 | 27 | 60
        chorizo | 455 | 24 | 2 | 38
        lamb;lamb mince;lamb shoulder;janjetina | 250 | 17 | 0 | 20
        salmon;losos | 208 | 20 | 0 | 13 | 150
        tuna;tuna konzerva | 120 | 26 | 0 | 1
        cod;haddock;hake;white fish;fish fillet;fish;bakalar;oslic;riba | 82 | 18 | 0 | 0.7 | 150
        fish sauce | 35 | 5 | 4 | 0
        prawn;shrimp;king prawn;kozice;skampi | 99 | 24 | 0.2 | 0.3
        egg yolk;zumanjak | 322 | 16 | 3.6 | 27 | 17
        egg white;bjelanjak | 52 | 11 | 0.7 | 0.2 | 33
        egg;jaje | 143 | 12.6 | 0.7 | 9.5 | 50
        milk;whole milk;semi skimmed milk;mlijeko | 50 | 3.4 | 4.8 | 2
        double cream;heavy cream;whipping cream;single cream;cream;slatko vrhnje;vrhnje za kuhanje | 340 | 2 | 3 | 36
        sour cream;soured cream;kiselo vrhnje | 195 | 3 | 4 | 19
        butter;maslac | 717 | 0.9 | 0.1 | 81 | | | 227
        cheddar;cheese;gouda;edam;gruyere;sir;trapist | 380 | 25 | 1.3 | 31 | | | 110
        parmesan;pecorino;parmezan | 431 | 38 | 4 | 29 | | | 100
        mozzarella | 280 | 22 | 2.2 | 22
        feta | 264 | 14 | 4 | 21
        cream cheese;mascarpone;krem sir | 340 | 6 | 4 | 34
        cottage cheese;svjezi sir;skuta | 100 | 11 | 3.4 | 4.5
        greek yogurt;greek yoghurt;grcki jogurt | 97 | 9 | 4 | 5
        yogurt;yoghurt;natural yoghurt;jogurt | 61 | 3.5 | 4.7 | 3.3
        rice;basmati rice;basmati;jasmine rice;long grain rice;arborio rice;riza | 360 | 7 | 79 | 0.6 | | | 185
        pasta;spaghetti;penne;macaroni;fusilli;tagliatelle;lasagne;linguine;rigatoni;farfalle;noodle;egg noodle;rice noodle;tjestenina;makaroni;spageti;rezanci | 360 | 12.5 | 72 | 1.5
        flour;plain flour;self raising flour;all purpose flour;brasno;psenicno brasno | 364 | 10 | 76 | 1 | | | 125
        bread;white bread;toast;bun;brioche;kruh | 265 | 9 | 49 | 3.2 | 30
        breadcrumb;panko;prezle | 395 | 13 | 72 | 5 | | | 110
        oat;rolled oats;porridge oats;zobene pahuljice | 389 | 17 | 66 | 7 | | | 90
        couscous;kus kus | 376 | 13 | 77 | 0.6 | | | 170
        bulgur;quinoa;kvinoja | 365 | 13 | 66 | 3
        tortilla;wrap;tortilla wrap | 300 | 8 | 50 | 7 | 45
        polenta;cornmeal;kukuruzno brasno | 360 | 7 | 77 | 1.2
        cornstarch;cornflour;corn flour;skrob;gustin | 381 | 0.3 | 91 | 0.1 | | | 128
        sugar;caster sugar;brown sugar;icing sugar;granulated sugar;secer | 387 | 0 | 100 | 0 | | | 200
        potato;krumpir | 77 | 2 | 17 | 0.1 | 170
        sweet potato;batat | 86 | 1.6 | 20 | 0.1 | 130
        onion;red onion;white onion;shallot;luk;crveni luk | 40 | 1.1 | 9.3 | 0.1 | 110
        spring onion;scallion;mladi luk | 32 | 1.8 | 7.3 | 0.2 | 15
        garlic;garlic clove;cesnjak | 149 | 6.4 | 33 | 0.5 | 3
        carrot;mrkva | 41 | 0.9 | 10 | 0.2 | 70
        sun dried tomato;sundried tomato | 258 | 14 | 56 | 3 | 8
        tomato;cherry tomato;rajcica | 18 | 0.9 | 3.9 | 0.2 | 120
        tomato puree;tomato paste;tomato puree;koncentrat rajcice | 82 | 4.3 | 19 | 0.5
        chopped tomato;tinned tomato;canned tomato;plum tomato;passata;pelati;rajcica pelati | 24 | 1.2 | 4.5 | 0.1
        bell pepper;red pepper;green pepper;yellow pepper;orange pepper;capsicum;paprika;paprike | 31 | 1 | 6 | 0.3 | 150
        courgette;zucchini;tikvica | 17 | 1.2 | 3.1 | 0.3 | 200
        aubergine;eggplant;patlidzan | 25 | 1 | 6 | 0.2 | 300
        mushroom;champignon;gljive;sampinjoni | 22 | 3.1 | 3.3 | 0.3 | 20
        broccoli;brokula;brokoli | 34 | 2.8 | 7 | 0.4
        cauliflower;cvjetaca | 25 | 1.9 | 5 | 0.3
        spinach;spinac | 23 | 2.9 | 3.6 | 0.4
        cabbage;kupus | 25 | 1.3 | 5.8 | 0.1
        lettuce;salad leaves;salata | 15 | 1.4 | 2.9 | 0.2
        cucumber;krastavac | 15 | 0.7 | 3.6 | 0.1 | 200
        celery;celer | 16 | 0.7 | 3 | 0.2 | 40
        leek;poriluk | 61 | 1.5 | 14 | 0.3 | 90
        pea;grasak | 81 | 5.4 | 14 | 0.4
        green bean;runner bean;mahune | 31 | 1.8 | 7 | 0.2
        sweetcorn;corn;kukuruz | 86 | 3.3 | 19 | 1.4
        pumpkin;butternut squash;squash;bundeva | 26 | 1 | 6.5 | 0.1
        kale;kelj | 49 | 4.3 | 9 | 0.9
        beetroot;cikla | 43 | 1.6 | 10 | 0.2 | 80
        avocado;avokado | 160 | 2 | 9 | 15 | 150
        lemon;limun | 29 | 1.1 | 9.3 | 0.3 | 60
        lemon juice;sok od limuna | 22 | 0.4 | 6.9 | 0.2
        lime | 30 | 0.7 | 11 | 0.2 | 45
        orange;naranca | 47 | 0.9 | 12 | 0.1 | 150
        apple;jabuka | 52 | 0.3 | 14 | 0.2 | 180
        banana | 89 | 1.1 | 23 | 0.3 | 120
        mango | 60 | 0.8 | 15 | 0.4
        pineapple;ananas | 50 | 0.5 | 13 | 0.1
        strawberry;blueberry;raspberry;berry;jagode;borovnice | 45 | 0.8 | 10 | 0.4
        raisin;sultana;dried fruit;grozdice | 300 | 3 | 79 | 0.5
        ginger;djumbir | 80 | 1.8 | 18 | 0.8
        kidney bean;black bean;cannellini bean;haricot bean;butter bean;borlotti bean;baked bean;bean;grah | 90 | 6 | 15 | 0.5
        chickpea;garbanzo bean;slanutak | 120 | 7 | 20 | 2.5
        lentil;red lentil;green lentil;leca | 350 | 25 | 60 | 1.1
        tofu | 76 | 8 | 1.9 | 4.8
        olive oil;vegetable oil;sunflower oil;rapeseed oil;coconut oil;oil;maslinovo ulje;ulje | 884 | 0 | 0 | 100 | | o
        mayonnaise;majoneza | 680 | 1 | 0.6 | 75
        peanut butter | 588 | 25 | 20 | 50
        pesto | 460 | 5 | 6 | 46
        nut;almond;walnut;cashew;peanut;pine nut;hazelnut;badem;orasi;ljesnjaci | 600 | 18 | 20 | 52 | | | 140
        olive;masline | 115 | 0.8 | 6 | 11 | 4
        stock;chicken stock;beef stock;vegetable stock;fish stock;broth;juha;temeljac | 8 | 1 | 0.5 | 0.2
        water;voda | 0 | 0 | 0 | 0
        wine;white wine;red wine;sherry;brandy;vino | 85 | 0.1 | 2.6 | 0
        beer;pivo | 43 | 0.5 | 3.6 | 0
        soy sauce;soya sauce;tamari;sojin umak | 53 | 8 | 5 | 0.6
        honey;maple syrup;syrup;med | 304 | 0.3 | 82 | 0 | | | 340
        coconut milk;coconut cream;kokosovo mlijeko | 197 | 2 | 3 | 21
        ketchup;tomato ketchup | 100 | 1 | 26 | 0.1
        mustard;dijon mustard;senf | 66 | 4 | 6 | 3.3
        vinegar;balsamic vinegar;wine vinegar;ocat | 20 | 0 | 0.6 | 0
        tomato sauce;pasta sauce | 50 | 1.5 | 9 | 1
        cocoa;cocoa powder;kakao | 228 | 20 | 58 | 14
        chocolate;dark chocolate;chocolate chips;cokolada | 546 | 5 | 60 | 31
        jam;dzem | 250 | 0.4 | 60 | 0.1
        salt;sea salt;sol;pepper;black pepper;white pepper;crni papar;papar;oregano;basil;bosiljak;parsley;persin;thyme;timijan;rosemary;ruzmarin;bay leaf;lovor;cumin;kumin;coriander;cilantro;korijander;smoked paprika;paprika powder;chilli powder;chili powder;cayenne pepper;cayenne;chilli flake;chili flake;red pepper flake;chilli;chili;cinnamon;cimet;nutmeg;turmeric;kurkuma;curry powder;garam masala;mixed herb;italian seasoning;dill;kopar;mint;metvica;chive;vegeta;spice;mixed spice;baking powder;baking soda;bicarbonate of soda;vanilla;vanilla extract;saffron;clove;ground clove;allspice;sage;tarragon;fennel seed;mustard seed;sesame seed;cardamom;star anise;worcestershire sauce;tabasco;hot sauce;caper | 250 | 10 | 50 | 5 | | s
        """;

    private sealed record Indexed(FoodEntry Entry, string[][] Aliases);

    private static readonly Indexed[] Index = Build();

    private static Indexed[] Build()
    {
        var list = new List<Indexed>();
        foreach (var line in Raw.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = line.Split('|', StringSplitOptions.TrimEntries);
            var names = parts[0].Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            double? piece = parts.Length > 5 && parts[5].Length > 0 ? Num(parts[5]) : null;
            var flags = parts.Length > 6 ? parts[6] : "";
            double? cup = parts.Length > 7 && parts[7].Length > 0 ? Num(parts[7]) : null;
            var entry = new FoodEntry(names[0], Num(parts[1]), Num(parts[2]), Num(parts[3]), Num(parts[4]), piece,
                flags.Contains('s'), cup, flags.Contains('o'));
            list.Add(new Indexed(entry, [.. names.Select(TextNorm.Tokens)]));
        }
        return [.. list];
    }

    private static double Num(string s) => double.Parse(s, CultureInfo.InvariantCulture);

    /// <summary>Best match for an ingredient name: the entry whose alias covers the most words of the name.</summary>
    public static FoodEntry? Find(string? ingredientName)
    {
        if (string.IsNullOrWhiteSpace(ingredientName)) return null;
        var tokens = TextNorm.Tokens(ingredientName);
        if (tokens.Length == 0) return null;

        FoodEntry? best = null;
        var bestScore = 0;
        foreach (var item in Index)
        {
            foreach (var alias in item.Aliases)
            {
                if (alias.Length == 0 || !ContainsSequence(tokens, alias)) continue;
                var score = alias.Length * 100 + alias.Sum(t => t.Length);
                if (score > bestScore) { bestScore = score; best = item.Entry; }
            }
        }
        return best;
    }

    private static bool ContainsSequence(string[] haystack, string[] needle)
    {
        for (var i = 0; i + needle.Length <= haystack.Length; i++)
        {
            var ok = true;
            for (var j = 0; j < needle.Length && ok; j++)
                ok = haystack[i + j] == needle[j];
            if (ok) return true;
        }
        return false;
    }
}

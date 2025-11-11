surgery-special-organ-manipulation-name = Organ Manipulation
surgery-special-organ-manipulation-desc = Enables the manipulation of organs inside the {$part}.

surgery-special-change-sex-name = Change Sex
surgery-special-change-sex-swap-desc = Swaps the patients current sex.
surgery-special-change-sex-desc = Changes the patients sex to {$sex ->
    [male] male
    [female] female
    *[unsexed] unsexed
}.

surgery-special-magic-mirror-name = Appearance Change
surgery-special-magic-mirror-desc = Allows changing the patients appearance.

surgery-special-item-slot-name = Item Slot
surgery-special-item-slot-desc = Allows removing or adding an item to a slot.

surgery-special-body-damage-name =
    { $deltasign ->
        [-1] Heal Organ Damage
        *[1] Deal Organ Damage
    }
surgery-special-body-damage-desc =
    { $deltasign ->
        [-1] Heals {NATURALFIXED($amount, 2)} organ damage
        *[1] Deals {NATURALFIXED($amount, 2)} organ damage
    }

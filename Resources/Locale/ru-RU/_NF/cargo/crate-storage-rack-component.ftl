crate-storage-rack-examine =
    { $count ->
        [0] Стойка пуста.
        [1] На стойке лежит { $count } предмет:
        [few] На стойке лежит { $count } предмета:
       *[others] На стойке лежит { $count } предметов:
    }

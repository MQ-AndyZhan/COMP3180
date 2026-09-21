-> Scene1

=== Scene1 === 
FRIEND: Hello, how is your day? 
    * [Great, how about you?] YOU: Great, how about you?
       FRIEND: I'm very enegetic, Thank you!
        -> Scene2
    * [Not so bad.] YOU: Not so bad.
        FRIEND: I'm glad to hear that!
        -> Scene2
    * [Awful, you never know!] YOU: Awful, you never know!
        FRIEND: Oh! Hope everything will get well soon!
        -> Scene2

-> DONE


=== Scene2 ===


FRIEND: By the way, what's your favorite food?
    * [Pizza] YOU: Pizza.
        -> GetFavoriteFood("pizza")
    * [Cake] YOU: Cake.
        -> GetFavoriteFood("cake")
    * [Noodles] YOU: Noodles.
        -> GetFavoriteFood("noodles")

-> DONE

=== GetFavoriteFood(favoriteFood) ===
FRIEND: I like {favoriteFood} as well!
Nice to talk with you, see you later!

-> END
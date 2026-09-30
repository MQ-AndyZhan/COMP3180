-> Scene1

=== Scene1 === 
Hello, how is your day? # Speaker:Friend
    * [Great, how about you?] Great, how about you? #Speaker:You
        I'm very enegetic, Thank you! # Speaker:Friend
        -> Scene2
    * [Not so bad.] Not so bad. #Speaker:You
        I'm glad to hear that! # Speaker:Friend
        -> Scene2
    * [Awful, you never know!] Awful, you never know! #Speaker:You
        Oh! Hope everything will get well soon!# Speaker:Friend
        -> Scene2

-> DONE


=== Scene2 ===


By the way, what's your favorite food? what's your favorite food? what's your favorite food? what's your favorite food?  #Speaker:Friend
    * [Pizza] Pizza. #Speaker:You
        -> GetFavoriteFood("pizza")
    * [Cake] Cake. #Speaker:You
        -> GetFavoriteFood("cake")
    * [Noodles] Noodles. #Speaker:You
        -> GetFavoriteFood("noodles")
-> DONE

=== GetFavoriteFood(favoriteFood) ===
I like {favoriteFood} as well! #Speaker:Friend
Nice to talk with you, see you later! #Speaker:Friend

-> END
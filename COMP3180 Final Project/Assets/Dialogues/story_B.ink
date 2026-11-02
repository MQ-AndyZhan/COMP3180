-> start

=== start ===
It was Friday afternoon, after school. Chris had just finished the homework he hadn’t completed the day before with his teacher and was about to head home. # speaker:narrator # line:intro_01
As he passed the art classroom, he spotted a girl sitting alone, staring intently at a sheet of paper. She was moving her pen across it, as though she were writing something. Curious, Chris stepped inside. # speaker:narrator # line:intro_02
Hey, what are you doing here all by yourself after school? Aren’t you going home? # speaker:player # line:intro_03
Startled by someone suddenly entering the room, the girl hurriedly began packing up her things, as though she were trying to hide something. # speaker:narrator # line:intro_04
Just as she was about to put away the sheet of paper, a sudden gust of wind blew it right in front of Chris. He picked it up. # speaker:narrator # line:intro_05
Uh... um... I didn’t draw that. # speaker:friend # line:intro_06
Chris took a closer look at the paper. It seemed to be a drawing of a riverside landscape, complete with a pond, mountains, grass, and lotus flowers. In the pond was a strange, polygon-shaped creature. # speaker:narrator # line:q01 # choice_point:cp01
* [Oh, you can draw? That’s amazing!] -> cp01_o1
* [If it isn’t yours, why did you have it?] -> cp01_o2
* [Then who drew it? It looks really nice.] -> cp01_o3

=== cp01_o1 ===
N-no... I can’t draw. # speaker:friend # line:cp01_o1_selected
Really? But I just watched you working on that drawing for ages. # speaker:player # line:cp01_o2_reply
-> cp01_after

=== cp01_o2 ===
B-because the person who drew it forgot to take it when they left. I was just going to write their name on it so nobody would mistake it for rubbish and throw it away. # speaker:friend # line:cp01_o2_selected
Is that so? But you’re the only person I’ve seen in here this whole time. # speaker:friend # line:cp01_o2_reply
-> cp01_after

=== cp01_o3 ===
M-my friend drew it. Pretty good, right? She just forgot to take it with her. # speaker:friend # line:cp01_o1_selected
It does look good. But why do you look so pleased about it?. # speaker:player # line:cp01_o2_selected
B-because I’m proud of my friend! # speaker:friend # line:cp01_o3_selected
-> cp01_after


=== cp01_after ===
How long were you watching from outside? # speaker:friend # line:shared_01_01
I saw you take that paper out of your bag. You drew it yourself, didn’t you? # speaker:player # line:shared_01_02
So you saw everything! Fine, I admit it. I drew it. # speaker:friend # line:shared_01_03
So it really was yours! Actually, I didn’t see where you got the paper from at all. Hahaha! # speaker:player # line:shared_01_04
You tricked me! # speaker:friend # line:shared_01_05
Hahaha, sorry, sorry! So why didn’t you want to admit that you drew it? # speaker:player # line:shared_01_06
Because I was afraid it didn’t look good. You saw it yourself, right? All the lines are crooked, and there are marks I can’t erase because I pressed too hard with the pencil. # speaker:friend # line:shared_01_07
And it doesn’t really look like what it’s supposed to be. # speaker:friend # line:q02 # choice_point:cp02
* [I actually think it looks really good.] -> cp02_o1
* [I’m sure you’ll get better if you keep practising.] -> cp02_o2
* [Only a few parts look a little odd. The rest is pretty good.] -> cp02_o3


=== cp02_o1 ===
R-really? Thank you. # speaker:friend # line:cp02_o1_selected
You are welcome! # speaker:player # line:cp02_o1_reply
-> cp02_after

=== cp02_o2 ===
If only I had that much time to practise. # speaker:friend # line:cp02_o2_selected
Why don’t you have time? # speaker:player # line:cp02_o2_reply
-> cp02_after

=== cp02_o3 ===
See! Even you think there’s something wrong with it! # speaker:friend # line:cp02_o1_selected
But that doesn’t stop the drawing from looking good overall? # speaker:player  # line:cp02_o2_selected
You’re lying again! # speaker:friend # line:cp02_o3_selected
Nope, I'm not. # speaker:player # line:cp02_o4_selected
-> cp02_after

=== cp02_after ===
Sigh... The truth is, my family doesn’t support me studying art seriously. They think it won’t help me get a job in the future. # speaker:friend # line:shared_02_01
Huh? How can they be so sure what kind of job you’ll have? # speaker:player # line:shared_02_02
I guess they just want me to become a doctor or a lawyer. # speaker:friend # line:shared_02_03
Could you give me my paper back? I’d like to draw a little more. # speaker:friend # line:shared_02_04
Huh? Oh. Sure. # speaker:player # line:shared_02_05
Chris sat down beside Tracy, and they continued drawing and chatting. # speaker:narrator # line:shared_02_06
So, to make sure I can get into one of those careers they keep talking about, I have a private tutor come to my house every day after school. # speaker:friend # line:shared_02_07
By the time I finish tutoring and all my school homework, it’s pretty much bedtime. # speaker:friend # line:shared_02_08
Then what about today...? # speaker:player # line:shared_02_09
My tutor couldn’t come today because something else came up, so the session was cancelled. They still left me homework, though. # speaker:friend # line:shared_02_10
Classes finished early today too, so I finally got a chance to draw a little longer. To be honest, I almost don’t want to go home. # speaker:friend # line:shared_02_11
You really love drawing, huh? If I had to study that hard, I’d probably go straight out with my friends instead, especially if school finished early. Speaking of which, where are your friends? # speaker:player # line:shared_02_12
They’re probably browsing around the supermarket on their way home. Or maybe they went to karaoke. # speaker:friend # line:q03 # choice_point:cp03
* [Why didn’t they invite you? Doesn’t sound like a very close friendship.] -> cp03_o1
* [It must feel great not having tutoring today.] -> cp03_o2
* [Why not go join them? Spending time with your friends is important too.] -> cp03_o3

=== cp03_o1 ===
They did invite me, but I don’t really like group activities, so I turned them down. # speaker:friend # line:cp03_o1_selected
Oh, okay. Forget I said anything. # speaker:player # line:cp03_o1_reply
-> cp03_after

=== cp03_o2 ===
Yeah. Maybe if I had more days like this, I’d be better at drawing by now. # speaker:friend # line:cp03_o2_selected
You’re so keen to draw. I’m sure you’ll improve with time. # speaker:player # line:cp03_o2_reply
-> cp03_after

=== cp03_o3 ===
We’re really just classmates who happen to be in the same classroom every day. Outside class, we all do our own thing. So we’re not actually that close. Haha. # speaker:friend # line:cp03_o1_selected
Well, if you can get closer to them, you should try. That way, if you ever need their help, it’ll be easier to ask. # speaker:player # line:cp03_o2_selected
That makes a lot of sense. I’ll try! # speaker:friend # line:cp03_o3_selected
-> cp03_after

=== cp03_after ===
Actually, there’s something I’ve been meaning to ask. What’s that polygon-shaped thing in the middle of your pond? # speaker:player # line:shared_03_01
It’s a duck. # speaker:friend # line:shared_03_02
Huh? I thought it was a rock. # speaker:player # line:shared_03_03
Nope. If you want to draw something accurately, you have to start with its outline and then gradually add the details inside it.' # speaker:friend # line:shared_03_04
I see. Then how did you draw that mountain in the background? # speaker:player # line:shared_03_05
The mountain? That’s easy. The process is pretty similar to drawing the duck. The important thing is to make sure the peaks and dips in its outline don’t look too repetitive. # speaker:friend # line:shared_03_06
For example, if one peak is 3 cm high, the next could be 1 cm, then 2.5 cm, or you could make one wider than the previous two. # speaker:friend # line:shared_03_07
Got it. Then could you tell me how you drew the water? # speaker:player # line:shared_03_08
Of course! There are a few things to keep in mind. I wanted the water to look clear enough to see the bottom, so I started by drawing the objects underwater. # speaker:friend # line:shared_03_09
Ripples distort the way those objects look, so you can make their lines a little wavy too. And since light can pass through the surface and spread underwater, you shouldn’t shade the whole thing too dark. # speaker:player # line:shared_03_10
Finally, I added the ripples on the surface where the duck had been swimming. # speaker:friend # line:shared_03_11
Chris suddenly noticed that Tracy was getting happier and happier as she talked. She seemed much more cheerful than when they first met. # speaker:narrator # line:shared_03_12
That’s a really detailed explanation. Can I ask one last question? # speaker:player # line:shared_03_13
Of course, of course! Go ahead! # speaker:friend # line:shared_03_14
Why are you suddenly so happy? # speaker:player # line:shared_03_15
Because someone’s actually willing to talk to me about drawing! I’ve never talked about it with my friends before because I’m afraid they won’t be interested, or that they’ll find me annoying. # speaker:friend # line:q04 # choice_point:cp04
* [Aren’t you worried I might find you annoying too?] -> cp04_o1
* [How would you know if you’ve never tried?] -> cp04_o2
* [You could talk about drawing with your family.?] -> cp04_o3

=== cp04_o1 ===
Uh... so you already think I’m annoying... I’m sorry.? # speaker:friend # line:cp04_o1_selected
No, no! I was only joking. # speaker:player # line:cp04_o1_reply
-> cp04_after

=== cp04_o2 ===
I’d rather not risk it. What if they really do find me annoying? I don’t have many friends to begin with. # speaker:friend # line:cp04_o2_selected
Your friendships really are fragile, huh? # speaker:player # line:cp04_o2_reply
-> cp04_after

=== cp04_o3 ===
I’m an only child, and all my parents ever do is work and nag me to finish my homework. # speaker:friend # line:cp04_o3_selected
That’s rough. # speaker:player # line:cp04_o3_reply
-> cp04_after

=== cp04_after ===
By the way, did you really understand everything I was talking about earlier? # speaker:friend # line:shared_04_01
Of course... not. # speaker:player # line:shared_04_02
Huh? Weren’t you clever enough to trick me into admitting the truth earlier? Hahaha!. # speaker:friend # line:shared_04_04
That’s only because you don’t talk to people much. You didn’t catch on. # speaker:player # line:shared_04_05
Then you should talk to me more often. # speaker:friend # line:shared_04_06
I’ll pass. I’d rather hang out with my mates. # speaker:player # line:shared_04_07
How cold! We’ve been talking for so long, and I still don’t even know your name. # speaker:friend # line:shared_04_08
Chris. # speaker:player # line:shared_04_09
I’m Tracy. Thanks for talking with me for so long! It felt really good to finally say all the things I’ve been keeping to myself. # speaker:friend # line:shared_04_10
No problem. I’ll get going, then. # speaker:player # line:shared_04_11
Wait! You’ve asked me so many questions. Can I ask you one? # speaker:friend # line:shared_04_12
Go ahead. # speaker:player # line:shared_04_13
Why did you decide to come in and talk to me? # speaker:friend # line:shared_04_14
I guess I was curious. I hardly ever go into the art classroom. # speaker:player # line:shared_04_15
Is that really all? Are you sure you didn’t fall for me while you were sneaking a look? # speaker:friend # line:shared_04_16
You’re imagining things. I’m leaving. # speaker:player # line:shared_04_17
Hahaha! Don’t go! Stay and chat a little longer~ # speaker:friend # line:shared_04_18
Chris walked home alone. # speaker:narrator # line:shared_04_19
She looked so innocent, but she’s actually pretty sly. Guess you really can’t judge a book by its cover. # speaker:player # line:ending_last # end:common
-> END

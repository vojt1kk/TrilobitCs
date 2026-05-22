# TrilobitCS — databázové schéma

DBML snapshot aktuálních EF Core entit. Vlož do [dbdiagram.io](https://dbdiagram.io/d).

Enumy jsou v PostgreSQL uložené jako `int` (EF Core default) — hodnoty viz `note:` u sloupce.

## DBML

```dbml
// =====================
// USERS
// =====================
Table users {
  id int [pk, increment]
  nickname varchar(50) [unique, not null]
  first_name varchar(100) [not null]
  last_name varchar(100) [not null]
  email varchar(100) [unique, not null]
  password varchar(255) [not null]
  gender int [not null, note: '0=Male, 1=Female, 2=Other']
  birth_date date [not null]
  profile_picture varchar(255)
  role int [default: 0, note: '0=User, 1=Leader']
  organisation_id int [note: 'null = bez organizace']
  created_at timestamp [default: `now()`]
}

Table refresh_tokens {
  id int [pk, increment]
  token varchar(255) [not null]
  user_id int [not null]
  expires_at timestamp [not null]
  created_at timestamp [not null]
  revoked_at timestamp [note: 'null = aktivní']
}

Table followers {
  id int [pk, increment]
  follower_id int [not null]
  following_id int [not null]
  created_at timestamp [default: `now()`]

  indexes {
    (follower_id, following_id) [unique]
  }

  note: 'CHECK constraint v migraci: follower_id != following_id'
}

// =====================
// EAGLE FEATHERS
// =====================
Table eagle_feathers {
  id int [pk, increment]
  light smallint [not null, note: '1=1.světlo ... 4=4.světlo']
  section varchar(10) [not null, note: '1A, 1B, 2A...']
  number smallint [not null, note: 'pořadí v rámci sekce']
  name varchar(150) [not null]
  challenge text [not null, note: 'znění ČINU']
  grand_challenge text [not null, note: 'znění VELKÉHO ČINU']
  source_url varchar(255) [not null]
  created_at timestamp [default: `now()`]
  updated_at timestamp [default: `now()`]

  indexes {
    (light, section, number) [unique]
  }
}

Table user_eagle_feathers {
  id int [pk, increment]
  user_id int [not null]
  eagle_feather_id int [not null]
  is_grand_challenge boolean [default: false, note: 'false=čin, true=velký čin']
  is_completed boolean [default: false, note: 'true při vytvoření postu, false při smazání postu']
  status int [default: 0, note: '0=Pending, 1=Approved, 2=Rejected']
  verified_by int [note: 'FK -> users (leader)']
  earned_at timestamp
  created_at timestamp [default: `now()`]

  indexes {
    (user_id, eagle_feather_id) [unique]
  }
}

// =====================
// CHALLENGES
// =====================
Table challenges {
  id int [pk, increment]
  eagle_feather_id int [not null, note: 'náhodně vybrané orli pero pro tuto challenge']
  title varchar(150) [not null]
  description text
  difficulty_level int [note: '1-3 hvězdičky']
  valid_from timestamp
  valid_to timestamp
  created_at timestamp [default: `now()`]
}

Table user_challenges {
  id int [pk, increment]
  user_id int [not null]
  challenge_id int [not null]
  pinned_at timestamp [not null, default: `now()`]
  unpinned_at timestamp [note: 'null = aktuálně pinnuto']

  indexes {
    (user_id, challenge_id) [unique]
  }
}

// =====================
// ORGANISATIONS
// =====================
Table organisations {
  id int [pk, increment]
  name varchar(100) [not null]
  description text
  avatar_url varchar(255)
  leader_id int [not null, note: 'FK -> users (role=Leader), ON DELETE RESTRICT']
  created_at timestamp [default: `now()`]
}

Table organisation_invites {
  id int [pk, increment]
  organisation_id int [not null]
  invited_user_id int [not null]
  invited_by_id int [note: 'null pokud leader byl smazán']
  status int [default: 0, note: '0=Pending, 1=Accepted, 2=Declined']
  accepted_at timestamp [note: 'null = nebylo přijato']
  declined_at timestamp [note: 'null = nebylo odmítnuto']
  created_at timestamp [default: `now()`]

  indexes {
    (invited_user_id, organisation_id, declined_at) [unique, note: 'PostgreSQL NULL!=NULL: pending (declined_at=NULL) koexistuje s declined (declined_at!=NULL)']
  }
}

// =====================
// POSTS
// =====================
Table posts {
  id int [pk, increment]
  user_id int [not null]
  organisation_id int [note: 'null = jen pro followers']
  user_eagle_feather_id int [not null, note: 'vždy přítomné — post je vždy o konkrétním EF']
  challenge_id int [note: 'null = post nevznikl přes challenge']
  content text
  image_url varchar(255)
  created_at timestamp [default: `now()`]
}

// =====================
// LIKES (polymorfní — čistý)
// =====================
Table likes {
  id int [pk, increment]
  user_id int [not null]
  likeable_type int [not null, note: '0=Posts, 1=Comments']
  likeable_id int [not null, note: 'FK není vynuceno DB — zajišťuje aplikační vrstva']
  created_at timestamp [default: `now()`]

  indexes {
    (user_id, likeable_type, likeable_id) [unique]
    (likeable_type, likeable_id)
  }
}

// =====================
// COMMENTS (polymorfní — čistý)
// =====================
Table comments {
  id int [pk, increment]
  user_id int [not null]
  commentable_type int [not null, note: '0=Posts, 1=Comments']
  commentable_id int [not null, note: 'FK není vynuceno DB — zajišťuje aplikační vrstva']
  content text [not null]
  created_at timestamp [default: `now()`]

  indexes {
    (commentable_type, commentable_id)
  }
}

// =====================
// GROUPS
// =====================
TableGroup auth {
  users
  refresh_tokens
}

TableGroup social {
  followers
}

TableGroup feathers {
  eagle_feathers
  user_eagle_feathers
}

TableGroup challenges_group {
  challenges
  user_challenges
}

TableGroup org {
  organisations
  organisation_invites
}

TableGroup feed {
  posts
  likes
  comments
}

// =====================
// REFS
// =====================
Ref: refresh_tokens.user_id > users.id

Ref: users.organisation_id > organisations.id
Ref: organisations.leader_id > users.id

Ref: followers.follower_id > users.id
Ref: followers.following_id > users.id

Ref: user_eagle_feathers.user_id > users.id
Ref: user_eagle_feathers.eagle_feather_id > eagle_feathers.id
Ref: user_eagle_feathers.verified_by > users.id

Ref: challenges.eagle_feather_id > eagle_feathers.id

Ref: user_challenges.user_id > users.id
Ref: user_challenges.challenge_id > challenges.id

Ref: organisation_invites.organisation_id > organisations.id
Ref: organisation_invites.invited_user_id > users.id
Ref: organisation_invites.invited_by_id > users.id

Ref: posts.user_id > users.id
Ref: posts.organisation_id > organisations.id
Ref: posts.user_eagle_feather_id > user_eagle_feathers.id
Ref: posts.challenge_id > challenges.id

Ref: likes.user_id > users.id
// likes.likeable_id — bez DB ref (polymorfní)

Ref: comments.user_id > users.id
// comments.commentable_id — bez DB ref (polymorfní)
```

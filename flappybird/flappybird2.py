import pygame
import sys
import random

pygame.init()
WIDTH, HEIGHT = 400, 600
FPS = 60

# game window
screen = pygame.display.set_mode((WIDTH, HEIGHT))
clock = pygame.time.Clock()
font = pygame.font.SysFont(None, 48)

# colors
WHITE = (255,255,255)
BG_COLOR = (135, 206, 250)   # sky blue
GROUND_COLOR = (222, 184, 135)  # sand
BULLET_COLOR = (255, 50, 50)
ENEMY_COLOR = (180, 0, 0)

# bird
BIRD_SIZE = 30
bird_x = WIDTH // 4
bird_y = HEIGHT // 2
bird_vel = 0
GRAVITY = 0.5
FLAP_STRENGTH = -7.5

# immunity buff
IMMUNE_DURATION = 2000   # milliseconds
immune_start_time = 0

# bird image
BIRD_IMG = pygame.image.load('bird.png').convert_alpha()
BIRD_IMG = pygame.transform.scale(BIRD_IMG, (BIRD_SIZE, BIRD_SIZE))

# pipes
PIPE_WIDTH = 60
PIPE_GAP = 200
pipe_list = []
SPAWNPIPE = pygame.USEREVENT
pygame.time.set_timer(SPAWNPIPE, 1600)

# enemies
ENEMY_WIDTH, ENEMY_HEIGHT = 40, 30
ENEMY_SPEED = 3
enemy_list = []
SPAWNENEMY = pygame.USEREVENT + 1
pygame.time.set_timer(SPAWNENEMY, 2000)

# bullets
player_bullets = []
enemy_bullets = []
BULLET_SPEED = 7
ENEMY_SHOOT = pygame.USEREVENT + 2
pygame.time.set_timer(ENEMY_SHOOT, 1500)

score = 0
game_active = True
scored_pipes = set()

def draw_bird(x, y, vel, immune):
    angle = -vel * 3
    rotated = pygame.transform.rotate(BIRD_IMG, angle)
    # change alpha if immune
    rotated.set_alpha(150 if immune else 255)
    rect = rotated.get_rect(center=(x + BIRD_SIZE/2, y + BIRD_SIZE/2))
    screen.blit(rotated, rect.topleft)

def create_pipe():
    gap_y = random.randint(100, HEIGHT - 200)
    top = pygame.Rect(WIDTH, 0, PIPE_WIDTH, gap_y)
    bottom = pygame.Rect(WIDTH, gap_y + PIPE_GAP, PIPE_WIDTH, HEIGHT - gap_y - PIPE_GAP)
    return top, bottom

def move_pipes(pipes):
    for p in pipes:
        p.x -= 4
    return [p for p in pipes if p.x + PIPE_WIDTH > 0]

def draw_pipes(pipes):
    for p in pipes:
        pygame.draw.rect(screen, (34,139,34), p)

def create_enemy():
    y = random.randint(50, HEIGHT - 150)
    return pygame.Rect(WIDTH, y, ENEMY_WIDTH, ENEMY_HEIGHT)

def move_enemies(enemies):
    for e in enemies:
        e.x -= ENEMY_SPEED
    return [e for e in enemies if e.x + ENEMY_WIDTH > 0]

def draw_enemies(enemies):
    for e in enemies:
        pygame.draw.rect(screen, ENEMY_COLOR, e)

def spawn_enemy_bullets(enemies):
    for e in enemies:
        bx = e.x
        by = e.y + ENEMY_HEIGHT//2
        enemy_bullets.append(pygame.Rect(bx, by, 6, 3))

def move_bullets(bullets, direction=1):
    for b in bullets:
        b.x += BULLET_SPEED * direction
    return [b for b in bullets if 0 <= b.x <= WIDTH]

def draw_bullets(bullets):
    for b in bullets:
        pygame.draw.rect(screen, BULLET_COLOR, b)

def check_collision(pipes, immune):
    global game_active
    bird_rect = pygame.Rect(bird_x, bird_y, BIRD_SIZE, BIRD_SIZE)
    # ground / ceiling always kill
    if bird_y <= 0 or bird_y + BIRD_SIZE >= HEIGHT - 50:
        game_active = False
    # pipes only when not immune
    if not immune:
        for p in pipes:
            if bird_rect.colliderect(p):
                game_active = False
    # enemy bullets always kill
    for b in enemy_bullets:
        if bird_rect.colliderect(b):
            game_active = False

def display_score(scr, current):
    score_surf = font.render(str(current), True, WHITE)
    scr.blit(score_surf, (WIDTH//2 - score_surf.get_width()//2, 20))

# main loop
while True:
    now = pygame.time.get_ticks()
    immune = (now - immune_start_time) <= IMMUNE_DURATION

    for event in pygame.event.get():
        if event.type == pygame.QUIT:
            pygame.quit()
            sys.exit()

        if game_active and event.type == pygame.KEYDOWN:
            if event.key == pygame.K_SPACE:
                bird_vel = FLAP_STRENGTH
            if event.key == pygame.K_f:
                bx = bird_x + BIRD_SIZE
                by = bird_y + BIRD_SIZE//2
                player_bullets.append(pygame.Rect(bx, by, 6, 3))

        if not game_active and event.type == pygame.KEYDOWN and event.key == pygame.K_SPACE:
            # reset
            game_active = True
            pipe_list.clear()
            enemy_list.clear()
            player_bullets.clear()
            enemy_bullets.clear()
            bird_y = HEIGHT // 2
            bird_vel = 0
            score = 0
            scored_pipes.clear()
            immune_start_time = 0

        if event.type == SPAWNPIPE and game_active:
            pipe_list.extend(create_pipe())
        if event.type == SPAWNENEMY and game_active:
            enemy_list.append(create_enemy())
        if event.type == ENEMY_SHOOT and game_active:
            spawn_enemy_bullets(enemy_list)

    screen.fill(BG_COLOR)
    pygame.draw.rect(screen, GROUND_COLOR, (0, HEIGHT-50, WIDTH, 50))

    if game_active:
        # physics & draw
        bird_vel += GRAVITY
        bird_y += bird_vel
        draw_bird(bird_x, bird_y, bird_vel, immune)

        # pipes
        pipe_list = move_pipes(pipe_list)
        draw_pipes(pipe_list)

        # enemies
        enemy_list = move_enemies(enemy_list)
        draw_enemies(enemy_list)

        # bullets
        player_bullets = move_bullets(player_bullets, 1)
        enemy_bullets = move_bullets(enemy_bullets, -1)
        draw_bullets(player_bullets)
        draw_bullets(enemy_bullets)

        # collisions
        check_collision(pipe_list, immune)

        # player bullets hitting enemies
        for b in player_bullets[:]:
            for e in enemy_list[:]:
                if b.colliderect(e):
                    player_bullets.remove(b)
                    enemy_list.remove(e)
                    score += 5
                    immune_start_time = now   # grant pipe immunity
                    break

        # pipe scoring
        PIPE_SPEED = 4
        for p in pipe_list:
            if p.y == 0 and (p.centerx < bird_x) and (p.centerx + PIPE_SPEED >= bird_x):
                score += 1

        display_score(screen, score)

    else:
        over_surf = font.render("Game Over! Score: " + str(score), True, WHITE)
        scr_x = WIDTH//2 - over_surf.get_width()//2
        scr_y = HEIGHT//2 - over_surf.get_height()//2
        screen.blit(over_surf, (scr_x, scr_y))

    pygame.display.update()
    clock.tick(FPS)

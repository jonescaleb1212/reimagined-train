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
BG_COLOR = (135, 206, 250)  # sky blue
GROUND_COLOR = (222, 184, 135)  # sand

# bird
BIRD_SIZE = 30
bird_x = WIDTH // 4
bird_y = HEIGHT // 2
bird_vel = 0
GRAVITY = 0.5
FLAP_STRENGTH = -7.5

# bird image
BIRD_IMG = pygame.image.load('Jessesun.png').convert_alpha()
BIRD_IMG = pygame.transform.scale(BIRD_IMG, (BIRD_SIZE, BIRD_SIZE))

# pipes
PIPE_WIDTH = 60
PIPE_GAP = 150
pipe_list = []
SPAWNPIPE = pygame.USEREVENT
pygame.time.set_timer(SPAWNPIPE, 1200)

score = 0
game_active = True
scored_pipes = set()    # track which pipes we’ve counted

def draw_bird(x, y,vel):
    angle = -vel * 3  # tweak multiplier for effect
    rotated = pygame.transform.rotate(BIRD_IMG, angle)
    rect = rotated.get_rect(center=(x + BIRD_SIZE/2, y + BIRD_SIZE/2))
    screen.blit(BIRD_IMG, (x, y))

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

def check_collision(pipes):
    global game_active
    bird_rect = pygame.Rect(bird_x, bird_y, BIRD_SIZE, BIRD_SIZE)
    # ground / ceiling
    if bird_y <= 0 or bird_y + BIRD_SIZE >= HEIGHT - 50:
        game_active = False
    # pipes
    for p in pipes:
        if bird_rect.colliderect(p):
            game_active = False

def display_score(scr, current):
    score_surf = font.render(str(current), True, WHITE)
    scr.blit(score_surf, (WIDTH//2 - score_surf.get_width()//2, 20))

# main loop
while True:
    for event in pygame.event.get():
        if event.type == pygame.QUIT:
            pygame.quit()
            sys.exit()
        if game_active and event.type == pygame.KEYDOWN and event.key == pygame.K_SPACE:
            bird_vel = FLAP_STRENGTH
        if not game_active and event.type == pygame.KEYDOWN and event.key == pygame.K_SPACE:
            # reset
            game_active = True
            pipe_list.clear()
            bird_y = HEIGHT // 2
            bird_vel = 0
            score = 0
            scored_pipes.clear()
        if event.type == SPAWNPIPE and game_active:
            pipe_list.extend(create_pipe())

    screen.fill(BG_COLOR)
    pygame.draw.rect(screen, GROUND_COLOR, (0, HEIGHT-50, WIDTH, 50))

    if game_active:
        # bird
        bird_vel += GRAVITY
        bird_y += bird_vel
        draw_bird(bird_x, bird_y,bird_vel)

        # pipes
        pipe_list = move_pipes(pipe_list)
        draw_pipes(pipe_list)

        # collision
        check_collision(pipe_list)

        # scoring: detect the exact crossing event
        PIPE_SPEED = 4   # should match your pipe movement speed
        for p in pipe_list:
            if p.y == 0:
                # if in the last frame it was still to the right but now it's left of the bird:
                if (p.centerx < bird_x) and (p.centerx + PIPE_SPEED >= bird_x):
                    score += 1
        display_score(screen, int(score))
    else:
        over_surf = font.render("Game Over", True, WHITE)
        scr_x = WIDTH//2 - over_surf.get_width()//2
        scr_y = HEIGHT//2 - over_surf.get_height()//2
        screen.blit(over_surf, (scr_x, scr_y))

    pygame.display.update()
    clock.tick(FPS)

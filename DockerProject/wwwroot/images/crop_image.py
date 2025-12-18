import os
import sys
from PIL import Image

def process_grid(image_path, output_base_dir="processed_images"):
    # Configurația grilei (5 rânduri, 6 coloane)
    ROWS = 5
    COLS = 6

    # Mapează coordonatele grilei (Rând, Coloană) la (Folder, Nume Fișier)
    mapping = [
        # --- RANDUL 0: RESTAURANTE ---
        (0, 0, "restaurants", "gordons-kitchen.jpg"),
        (0, 1, "restaurants", "jamies-italian.jpg"),
        (0, 2, "restaurants", "bella-pizza.jpg"),
        (0, 3, "restaurants", "golden-wok.jpg"),
        (0, 4, "restaurants", "petit-bistro.jpg"),
        (0, 5, "restaurants", "burger-palace.jpg"),

        # --- RANDUL 1: PRODUSE ---
        (1, 0, "products", "beef-wellington.jpg"),
        (1, 1, "products", "lobster.jpg"),
        (1, 2, "products", "caesar-salad.jpg"),
        (1, 3, "products", "toffee-pudding.jpg"),
        (1, 4, "products", "carbonara.jpg"),
        (1, 5, "products", "margherita.jpg"),

        # --- RANDUL 2: PRODUSE ---
        (2, 0, "products", "lasagna.jpg"),
        (2, 1, "products", "tiramisu.jpg"),
        (2, 2, "products", "quattro-formaggi.jpg"),
        (2, 3, "products", "pepperoni.jpg"),
        (2, 4, "products", "diavola.jpg"),
        (2, 5, "products", "caprese.jpg"),

        # --- RANDUL 3: PRODUSE ---
        (3, 0, "products", "kung-pao.jpg"),
        (3, 1, "products", "sweet-sour.jpg"),
        (3, 2, "products", "fried-rice.jpg"),
        (3, 3, "products", "spring-rolls.jpg"),
        (3, 4, "products", "coq-au-vin.jpg"),
        (3, 5, "products", "ratatouille.jpg"),

        # --- RANDUL 4: PRODUSE ---
        (4, 0, "products", "cheeseburger.jpg"),
        (4, 1, "products", "bacon-burger.jpg"),
        (4, 2, "products", "veggie-burger.jpg"),
        (4, 3, "products", "fries.jpg"),
        (4, 4, "products", "milkshake.jpg"),
    ]

    try:
        img = Image.open(image_path)
        width, height = img.size
        
        # Calculăm dimensiunea unei singure "plăci"
        tile_width = width / COLS
        tile_height = height / ROWS

        print(f"Imagine încărcată: {width}x{height}")
        # AICI ERA EROAREA TA (am adăugat ghilimelele):
        print(f"Dimensiune decupare: {tile_width:.2f}x{tile_height:.2f}")

        for row, col, folder, filename in mapping:
            # Calculăm coordonatele de decupare
            left = col * tile_width
            top = row * tile_height
            right = left + tile_width
            bottom = top + tile_height

            # Decupăm
            crop = img.crop((left, top, right, bottom))

            # Creăm folderul dacă nu există
            target_folder = os.path.join(output_base_dir, folder)
            if not os.path.exists(target_folder):
                os.makedirs(target_folder)
                print(f"Creat folder: {target_folder}")

            # Salvăm
            save_path = os.path.join(target_folder, filename)
            crop.save(save_path)
            print(f"Salvat: {save_path}")

        print("\n--- GATA! Toate imaginile au fost decupate. ---")

    except FileNotFoundError:
        print(f"EROARE: Nu am găsit fișierul '{image_path}'. Verifică numele.")
    except Exception as e:
        print(f"A apărut o eroare neașteptată: {e}")

if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Folosire: python crop_image.py <nume_imagine_grid.jpg>")
    else:
        # Modifică aici output-ul direct în proiectul tău dacă vrei
        OUTPUT_DIR = "processed_images" 
        
        input_image = sys.argv[1]
        process_grid(input_image, OUTPUT_DIR)
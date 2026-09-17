import {
  BoxGeometry,
  type BufferGeometry,
  CircleGeometry,
  CylinderGeometry,
  Group,
  Quaternion,
  SphereGeometry,
  TorusGeometry,
  Vector3,
} from 'three';

import {
  alignedTo,
  createRandom,
  lathe,
  merge,
  mesh,
  place,
  type ProfilePoint,
  surface,
  topHeightAt,
  topNormalAt,
} from '../modeling.ts';

// Real-world size in meters: about 12 cm wide and 10 cm tall, so AR shows the actual portion.

const UP = new Vector3(0, 1, 0);
const PATTY_RADIUS = 0.0565;
const PATTY_HEIGHT = 0.012;
const CHEESE_THICKNESS = 0.0022;

const bottomBunProfile: ProfilePoint[] = [
  [0, 0],
  [0.049, 0],
  [0.0535, 0.004],
  [0.0555, 0.011],
  [0.054, 0.017],
  [0.05, 0.02],
];

const topBunProfile: ProfilePoint[] = [
  [0, 0],
  [0.052, 0],
  [0.0563, 0.004],
  [0.0573, 0.012],
  [0.054, 0.024],
  [0.045, 0.036],
  [0.03, 0.0452],
  [0.013, 0.0496],
  [0, 0.0505],
];

export function smashBurger(): Group {
  const random = createRandom(385);
  const burger = new Group();
  burger.name = 'Classic Smash Burger';

  const crust = surface('Bun crust', '#c7843f', 0.55);
  const crumb = surface('Bun crumb', '#efd5a0', 0.9);
  const beef = surface('Smashed beef', '#4a2915', 0.9);
  const cheddar = surface('Cheddar', '#f3a531', 0.38);
  const onion = surface('Caramelized onion', '#8b4513', 0.35);
  const sesame = surface('Sesame', '#f2e3c4', 0.6);

  mesh(burger, 'Bottom bun', lathe(bottomBunProfile), crust);
  let y = 0.02;
  mesh(
    burger,
    'Bottom bun crumb',
    place(new CircleGeometry(0.05, 40).rotateX(-Math.PI / 2), new Vector3(0, y, 0)),
    crumb,
  );

  for (const [index, turn] of [0, 0.7].entries()) {
    mesh(burger, `Patty ${index + 1}`, place(patty(turn), new Vector3(0, y + PATTY_HEIGHT / 2, 0)), beef);
    y += PATTY_HEIGHT;

    const cheeseRotation = new Quaternion().setFromAxisAngle(UP, Math.PI / 4 + turn);
    mesh(
      burger,
      `Cheddar ${index + 1}`,
      place(cheeseSlice(), new Vector3(0, y + CHEESE_THICKNESS / 2, 0), cheeseRotation),
      cheddar,
    );
    y += CHEESE_THICKNESS;
  }

  mesh(burger, 'Caramelized onions', onions(random, y + 0.0014), onion);
  y += 0.0035;

  mesh(burger, 'Top bun', place(lathe(topBunProfile), new Vector3(0, y, 0)), crust);
  mesh(burger, 'Sesame seeds', sesameSeeds(random, y), sesame);

  return burger;
}

/** A smashed patty spreads unevenly and gets a lacy, crisp edge. */
function patty(turn: number): BufferGeometry {
  const geometry = new CylinderGeometry(PATTY_RADIUS, PATTY_RADIUS * 1.02, PATTY_HEIGHT, 64, 2);
  const position = geometry.getAttribute('position');

  for (let index = 0; index < position.count; index++) {
    const x = position.getX(index);
    const z = position.getZ(index);
    if (Math.hypot(x, z) < 1e-6) {
      continue;
    }

    const angle = Math.atan2(z, x) + turn;
    const middleRing = Math.abs(position.getY(index)) < 1e-6 ? 1.025 : 1;
    const edge =
      middleRing *
      (1 +
        0.045 * Math.sin(angle * 5) +
        0.025 * Math.sin(angle * 11 + 1.3) +
        0.008 * Math.sin(angle * 17 + 0.4));

    position.setXYZ(index, x * edge, position.getY(index), z * edge);
  }

  geometry.computeVertexNormals();
  return geometry;
}

/** A square slice whose corners hang over the patty and melt downwards. */
function cheeseSlice(): BufferGeometry {
  const geometry = new BoxGeometry(0.098, CHEESE_THICKNESS, 0.098, 14, 1, 14);
  const position = geometry.getAttribute('position');

  for (let index = 0; index < position.count; index++) {
    const overhang = Math.max(0, Math.hypot(position.getX(index), position.getZ(index)) - 0.05);
    position.setY(index, position.getY(index) - overhang * overhang * 18 - overhang * 0.25);
  }

  geometry.computeVertexNormals();
  return geometry;
}

function onions(random: () => number, y: number): BufferGeometry {
  return merge(
    Array.from({ length: 9 }, () => {
      const ring = new TorusGeometry(0.008 + random() * 0.009, 0.0016, 5, 16, Math.PI * (1 + random() * 0.8));
      const angle = random() * Math.PI * 2;
      const distance = Math.sqrt(random()) * 0.032;

      return place(
        ring.rotateX(Math.PI / 2),
        new Vector3(Math.cos(angle) * distance, y + random() * 0.0015, Math.sin(angle) * distance),
        new Quaternion().setFromAxisAngle(UP, random() * Math.PI * 2),
      );
    }),
  );
}

/** Seeds on a golden-angle spiral, each tilted to follow the dome of the bun. */
function sesameSeeds(random: () => number, bunBase: number): BufferGeometry {
  const count = 44;

  return merge(
    Array.from({ length: count }, (_, index) => {
      const radius = 0.046 * Math.sqrt((index + 0.5) / count) * (0.92 + random() * 0.08);
      const angle = index * 2.399963 + random() * 0.4;
      const position = new Vector3(
        Math.cos(angle) * radius,
        bunBase + topHeightAt(topBunProfile, radius) + 0.0005,
        Math.sin(angle) * radius,
      );

      return place(
        new SphereGeometry(1, 6, 4),
        position,
        alignedTo(topNormalAt(topBunProfile, radius, angle), random() * Math.PI),
        new Vector3(0.0026, 0.0009, 0.0015),
      );
    }),
  );
}

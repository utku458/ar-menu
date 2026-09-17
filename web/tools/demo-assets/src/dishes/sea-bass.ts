import {
  BufferGeometry,
  CircleGeometry,
  CylinderGeometry,
  ExtrudeGeometry,
  Float32BufferAttribute,
  Group,
  Quaternion,
  Shape,
  SphereGeometry,
  Vector3,
} from 'three';

import { createRandom, lathe, merge, mesh, place, type ProfilePoint, surface } from '../modeling.ts';

// Real-world size in meters: a whole grilled sea bass (about 26 cm with its tail) on a 28 cm plate.

const UP = new Vector3(0, 1, 0);
const PLATE_TOP = 0.0065;
const FISH_LENGTH = 0.21;
const FISH_GIRTH = 0.034;
const FISH_FLATNESS = 0.5;

/** Closed cross-section of the plate, from the center of its top surface around the rim and back underneath. */
const plateProfile: ProfilePoint[] = [
  [0, 0],
  [0.078, 0],
  [0.104, 0.0045],
  [0.132, 0.0125],
  [0.141, 0.0195],
  [0.137, 0.0215],
  [0.128, 0.019],
  [0.108, 0.012],
  [0.085, PLATE_TOP],
  [0, PLATE_TOP],
];

export function seaBass(): Group {
  const random = createRandom(750);
  const dish = new Group();
  dish.name = 'Grilled Sea Bass';

  mesh(dish, 'Plate', lathe(plateProfile, 64), surface('Glazed ceramic', '#f6f3ec', 0.22));

  const fish = new Group();
  fish.name = 'Sea bass';
  fish.rotation.y = -0.35;
  fish.position.set(-0.012, 0, 0.004);
  dish.add(fish);

  const centerHeight = PLATE_TOP + FISH_GIRTH * FISH_FLATNESS * 0.92;
  mesh(fish, 'Body', fishBody(centerHeight), surface('Grilled skin', '#b3ada0', 0.45));
  mesh(fish, 'Grill marks', grillMarks(centerHeight), surface('Char', '#2c1d14', 0.8));
  mesh(fish, 'Tail', tail(centerHeight), surface('Fin', '#8c9597', 0.5, true));
  mesh(
    fish,
    'Eye',
    place(
      new SphereGeometry(0.0052, 16, 12),
      new Vector3(0.079, surfaceHeight(0.079, 0, centerHeight) - 0.0012, 0),
    ),
    surface('Eye', '#111111', 0.15),
  );

  const rind = surface('Lemon rind', '#f1cc3f', 0.45);
  const pulp = surface('Lemon pulp', '#f7e79c', 0.3);
  const lemonSlices: [x: number, z: number, tilt: number][] = [
    [0.078, 0.064, 0.18],
    [0.058, 0.08, -0.12],
  ];
  for (const [index, [x, z, tilt]] of lemonSlices.entries()) {
    const rotation = new Quaternion().setFromAxisAngle(new Vector3(1, 0, 0.4).normalize(), tilt);
    const center = new Vector3(x, PLATE_TOP + 0.004 + index * 0.004, z);
    mesh(
      dish,
      `Lemon slice ${index + 1}`,
      place(new CylinderGeometry(0.023, 0.023, 0.005, 32), center, rotation),
      rind,
    );
    mesh(
      dish,
      `Lemon pulp ${index + 1}`,
      place(new CircleGeometry(0.0205, 32).rotateX(-Math.PI / 2).translate(0, 0.00255, 0), center, rotation),
      pulp,
    );
  }

  mesh(
    dish,
    'Cherry tomatoes',
    merge(
      [
        [-0.07, 0.07],
        [-0.052, 0.086],
        [-0.086, 0.052],
      ].map(([x, z]) =>
        place(
          new SphereGeometry(0.0115, 16, 12),
          new Vector3(x, PLATE_TOP + 0.0105, z),
          undefined,
          new Vector3(1, 0.92, 1),
        ),
      ),
    ),
    surface('Cherry tomato', '#cf3525', 0.28),
  );

  mesh(dish, 'Rocket', rocketLeaves(random), surface('Rocket', '#3f7d2c', 0.7, true));

  return dish;
}

/** Radius of the fish's round cross-section along its length (t = 0 at the tail, 1 at the mouth). */
function girthAt(t: number): number {
  return 0.0055 + (FISH_GIRTH - 0.0055) * Math.sin(Math.PI * Math.pow(t, 0.9));
}

/** The fish lies on its side: height above the plate of its upper flank at (x, z). */
function surfaceHeight(x: number, z: number, centerHeight: number): number {
  const radius = girthAt(x / FISH_LENGTH + 0.5);
  return centerHeight + FISH_FLATNESS * Math.sqrt(Math.max(0, radius * radius - z * z));
}

function fishBody(centerHeight: number): BufferGeometry {
  const samples = 32;
  const profile: ProfilePoint[] = [[0, -FISH_LENGTH / 2]];
  for (let index = 0; index <= samples; index++) {
    const t = index / samples;
    profile.push([girthAt(t), (t - 0.5) * FISH_LENGTH]);
  }
  profile.push([0, FISH_LENGTH / 2]);

  // Revolved around the vertical axis, then laid down along x and flattened: a spindle lying on its flank.
  return lathe(profile, 40)
    .rotateZ(-Math.PI / 2)
    .scale(1, FISH_FLATNESS, 1)
    .translate(0, centerHeight, 0);
}

/** Charred stripes that follow the curve of the flank. */
function grillMarks(centerHeight: number): BufferGeometry {
  const halfWidth = 0.0021;
  const direction = new Vector3(Math.cos(1.1), 0, Math.sin(1.1));
  const across = new Vector3(-direction.z, 0, direction.x);

  return merge(
    [-0.045, -0.015, 0.015, 0.045].map((offset) => {
      const positions: number[] = [];
      const steps = 24;
      for (let step = 0; step <= steps; step++) {
        const s = (step / steps - 0.5) * 0.06;
        for (const side of [-1, 1]) {
          const x = offset + direction.x * s + across.x * side * halfWidth;
          const z = direction.z * s + across.z * side * halfWidth;
          positions.push(x, surfaceHeight(x, z, centerHeight) + 0.0006, z);
        }
      }

      const indices: number[] = [];
      for (let step = 0; step < steps; step++) {
        const a = step * 2;
        // Counter-clockwise seen from above, so the stripes face up and are not culled.
        indices.push(a, a + 1, a + 2, a + 1, a + 3, a + 2);
      }

      const stripe = new BufferGeometry();
      stripe.setAttribute('position', new Float32BufferAttribute(positions, 3));
      stripe.setIndex(indices);
      stripe.computeVertexNormals();
      return stripe;
    }),
  );
}

/** A forked tail fin, lying flat like the fish. */
function tail(centerHeight: number): BufferGeometry {
  const root = -FISH_LENGTH / 2 + 0.004;
  const shape = new Shape()
    .moveTo(root, -0.007)
    .quadraticCurveTo(root - 0.03, -0.018, root - 0.05, -0.034)
    .quadraticCurveTo(root - 0.04, -0.012, root - 0.033, 0)
    .quadraticCurveTo(root - 0.04, 0.012, root - 0.05, 0.034)
    .quadraticCurveTo(root - 0.03, 0.018, root, 0.007)
    .closePath();

  return new ExtrudeGeometry(shape, { depth: 0.0022, bevelEnabled: false, curveSegments: 12 })
    .rotateX(Math.PI / 2)
    .translate(0, centerHeight + 0.0011, 0);
}

function rocketLeaves(random: () => number): BufferGeometry {
  return merge(
    Array.from({ length: 13 }, () => {
      const angle = 3.6 + random() * 1.2;
      const distance = 0.085 + random() * 0.025;
      const rotation = new Quaternion()
        .setFromAxisAngle(UP, random() * Math.PI * 2)
        .multiply(new Quaternion().setFromAxisAngle(new Vector3(1, 0, 0), (random() - 0.5) * 0.5));

      return place(
        new SphereGeometry(1, 8, 4),
        new Vector3(
          Math.cos(angle) * distance,
          PLATE_TOP + 0.003 + random() * 0.004,
          Math.sin(angle) * distance,
        ),
        rotation,
        new Vector3(0.019, 0.0012, 0.0068),
      );
    }),
  );
}

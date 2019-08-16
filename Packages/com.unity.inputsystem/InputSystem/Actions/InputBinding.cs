using System;
using System.Linq;
using System.Text;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Utilities;

////REVIEW: do we really need overridable processors and interactions?

// Downsides to the current approach:
// - Being able to address entire batches of controls through a single control is awesome. Especially
//   when combining it type-kind of queries (e.g. "<MyDevice>/<Button>"). However, it complicates things
//   in quite a few areas. There's quite a few bits in InputActionState that could be simplified if a
//   binding simply maps to a control.

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// A mapping of control input to an action.
    /// </summary>
    /// <remarks>
    /// Each binding represents a value received from controls (see <see cref="InputControl"/>).
    /// There are two main types of bindings: "normal" bindings and "composite" bindings.
    ///
    /// Normal bindings directly bind to control(s) by means of <see cref="path"/> which is a "control path"
    /// (see <see cref="InputControlPath"/> for details about how to form paths). At runtime, the
    /// path of such a binding may match none, one, or multiple controls. Each control matched by the
    /// path will feed input into the binding.
    ///
    /// Composite bindings do not bind to controls themselves. Instead, they receive their input
    /// from their "part" bindings and then return a value representing a "composition" of those
    /// inputs. What composition specifically is performed depends on the type of the composite.
    /// <see cref="Composites.AxisComposite"/>, for example, will return a floating-point axis value
    /// computed from the state of two buttons.
    ///
    /// The action that is triggered by a binding is determined by its <see cref="action"/> property.
    /// The resolution to an <see cref="InputAction"/> depends on where the binding is used. For example,
    /// bindings that are part of <see cref="InputActionMap.bindings"/> will resolve action names to
    /// actions in the same <see cref="InputActionMap"/>.
    ///
    ///
    /// A binding can also be used as a override specification. In that scenario, <see cref="path"/>,
    /// <see cref="action"/>, and <see cref="groups"/> become search criteria that can be used to
    /// find existing bindings, and <see cref="overridePath"/> becomes the path to override existing
    /// binding paths with.
    ///
    /// Finally, a binding can be used as a form of specifying a mask that matching bindings must
    /// comply to. For example, a binding that has only <see cref="groups"/> set to "Gamepad" and all
    /// other fields set to default can be used to mask for bindings in the "Gamepad" group.
    /// </remarks>
    [Serializable]
    public struct InputBinding : IEquatable<InputBinding>
    {
        public const char Separator = ';';
        internal const string kSeparatorString = ";";

        /// <summary>
        /// Optional name for the binding.
        /// </summary>
        /// <remarks>
        /// For bindings that <see cref="isPartOfComposite">are part of composites</see>, this is
        /// the name of the field on the binding composite object that should be initialized with
        /// the control target of the binding.
        /// </remarks>
        public string name
        {
            get => m_Name;
            set => m_Name = value;
        }

        public Guid id
        {
            get
            {
                if (m_Guid == Guid.Empty && !string.IsNullOrEmpty(m_Id))
                    m_Guid = new Guid(m_Id);
                return m_Guid;
            }
            set
            {
                m_Guid = value;
                m_Id = m_Guid.ToString();
            }
        }

        /// <summary>
        /// Control path being bound to.
        /// </summary>
        /// <remarks>
        /// If the binding is a composite (<see cref="isComposite"/>), the path is the composite
        /// string instead.
        /// </remarks>
        /// <example>
        /// <code>
        /// "/*/{PrimaryAction}"
        /// </code>
        /// </example>
        public string path
        {
            get => m_Path;
            set => m_Path = value;
        }

        /// <summary>
        /// If the binding is overridden, this is the overriding path.
        /// Otherwise it is null.
        /// </summary>
        /// <remarks>
        /// Not serialized as overrides are considered temporary, runtime-only state.
        /// </remarks>
        public string overridePath
        {
            get => m_OverridePath;
            set => m_OverridePath = value;
        }

        /// <summary>
        /// Optional list of interactions and their parameters.
        /// </summary>
        /// <example>
        /// <code>
        /// "tap,slowTap(duration=1.2)"
        /// </code>
        /// </example>
        public string interactions
        {
            get => m_Interactions;
            set => m_Interactions = value;
        }

        public string overrideInteractions
        {
            get => m_OverrideInteractions;
            set => m_OverrideInteractions = value;
        }

        /// <summary>
        /// Optional list of processors to apply to control values.
        /// </summary>
        /// <remarks>
        /// This list has the same format as <see cref="InputControlAttribute.processors"/>.
        /// </remarks>
        public string processors
        {
            get => m_Processors;
            set => m_Processors = value;
        }

        public string overrideProcessors
        {
            get => m_OverrideProcessors;
            set => m_OverrideProcessors = value;
        }

        // Optional group name. This can be used, for example, to divide bindings into
        // control schemes. So, the binding for keyboard&mouse on an action would have
        // "keyboard&mouse" as its group, the binding for "touch" would have "touch as
        // its group, and so on.
        //
        // Overriding bindings on actions that have multiple bindings is driven by
        // binding groups. This can also be used to have multiple binding for the same
        // device or control scheme and override a specific one.
        //
        // NOTE: What a group represents is not proscribed by the system. If a group is
        //       meant to represent a specific device type or combination of device types,
        //       this can be implemented on top of the system.
        //
        //       One good use case for groups is to mark up bindings that have a certain
        //       common meaning. Say, for example, you have a several binding chains
        //       (maybe even across different action sets) where the first binding in the
        //       chain always represents the same "interaction". Let's say it's the left
        //       trigger on the gamepad and it'll swap between a primary set of bindings
        //       on the four-button group on the gamepad and a secondary set. You could
        //       mark up every single use of the interaction ...
        public string groups
        {
            get => m_Groups;
            set => m_Groups = value;
        }

        /// <summary>
        /// Name or ID of the action triggered by the binding.
        /// </summary>
        /// <remarks>
        /// This is null if the binding does not trigger an action.
        ///
        /// For InputBindings that are used as filters, this can be a "mapName/actionName" combination
        /// or "mapName/*" to match all actions in the given map.
        /// </remarks>
        /// <seealso cref="InputAction.name"/>
        /// <seealso cref="InputAction.id"/>
        public string action
        {
            get => m_Action;
            set => m_Action = value;
        }

        ////TODO: make public when chained bindings are implemented fully
        internal bool chainWithPrevious
        {
            get => (m_Flags & Flags.ThisAndPreviousCombine) == Flags.ThisAndPreviousCombine;
            set
            {
                if (value)
                    m_Flags |= Flags.ThisAndPreviousCombine;
                else
                    m_Flags &= ~Flags.ThisAndPreviousCombine;
            }
        }

        public bool isComposite
        {
            get => (m_Flags & Flags.Composite) == Flags.Composite;
            set
            {
                if (value)
                    m_Flags |= Flags.Composite;
                else
                    m_Flags &= ~Flags.Composite;
            }
        }

        public bool isPartOfComposite
        {
            get => (m_Flags & Flags.PartOfComposite) == Flags.PartOfComposite;
            set
            {
                if (value)
                    m_Flags |= Flags.PartOfComposite;
                else
                    m_Flags &= ~Flags.PartOfComposite;
            }
        }

        public void GenerateId()
        {
            m_Guid = Guid.NewGuid();
            m_Id = m_Guid.ToString();
        }

        public static InputBinding MaskByGroup(string group)
        {
            if (string.IsNullOrEmpty(group))
                throw new ArgumentNullException(nameof(group));

            return new InputBinding {groups = group};
        }

        public static InputBinding MaskByGroups(params string[] groups)
        {
            if (groups == null)
                throw new ArgumentNullException(nameof(groups));

            return new InputBinding {groups = string.Join(kSeparatorString, groups.Where(x => !string.IsNullOrEmpty(x)))};
        }

        [SerializeField] private string m_Name;
        [SerializeField] internal string m_Id;
        [SerializeField] private string m_Path;
        [SerializeField] private string m_Interactions;
        [SerializeField] private string m_Processors;
        [SerializeField] private string m_Groups;
        [SerializeField] private string m_Action;
        [SerializeField] internal Flags m_Flags;

        [NonSerialized] private string m_OverridePath;
        [NonSerialized] private string m_OverrideInteractions;
        [NonSerialized] private string m_OverrideProcessors;
        ////REVIEW: do we actually need this or should we just convert from m_Id on the fly all the time?
        [NonSerialized] private Guid m_Guid;

        /// <summary>
        /// This is the bindings path which is effectively being used.
        /// </summary>
        /// <remarks>
        /// This is either <see cref="overridePath"/> if that is set, or <see cref="path"/> otherwise.
        /// </remarks>
        public string effectivePath => overridePath ?? path;

        /// <summary>
        /// This is the interaction config which is effectively being used.
        /// </summary>
        /// <remarks>
        /// This is either <see cref="overrideInteractions"/> if that is set, or <see cref="interactions"/> otherwise.
        /// </remarks>
        public string effectiveInteractions => overrideInteractions ?? interactions;

        /// <summary>
        /// This is the processor config which is effectively being used.
        /// </summary>
        /// <remarks>
        /// This is either <see cref="overrideProcessors"/> if that is set, or <see cref="processors"/> otherwise.
        /// </remarks>
        public string effectiveProcessors => overrideProcessors ?? processors;

        internal bool isEmpty =>
            string.IsNullOrEmpty(effectivePath) && string.IsNullOrEmpty(action) &&
            string.IsNullOrEmpty(groups);

        public bool Equals(InputBinding other)
        {
            return string.Equals(effectivePath, other.effectivePath) &&
                string.Equals(effectiveInteractions, other.effectiveInteractions) &&
                string.Equals(effectiveProcessors, other.effectiveProcessors) &&
                string.Equals(groups, other.groups) &&
                string.Equals(action, other.action);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;

            return obj is InputBinding && Equals((InputBinding)obj);
        }

        public static bool operator==(InputBinding left, InputBinding right)
        {
            return left.Equals(right);
        }

        public static bool operator!=(InputBinding left, InputBinding right)
        {
            return !(left == right);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (effectivePath != null ? effectivePath.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (effectiveInteractions != null ? effectiveInteractions.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (effectiveProcessors != null ? effectiveProcessors.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (groups != null ? groups.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (action != null ? action.GetHashCode() : 0);
                return hashCode;
            }
        }

        public override string ToString()
        {
            var builder = new StringBuilder();

            // Add action.
            if (!string.IsNullOrEmpty(action))
            {
                builder.Append(action);
                builder.Append(':');
            }

            // Add path.
            var path = effectivePath;
            if (!string.IsNullOrEmpty(path))
                builder.Append(path);

            // Add groups.
            if (!string.IsNullOrEmpty(groups))
            {
                builder.Append('[');
                builder.Append(groups);
                builder.Append(']');
            }

            return builder.ToString();
        }

        ////TODO: also support matching by name (taking the binding tree into account so that components
        ////      of composites can be referenced through their parent)

        ////TODO: this must be exposed; matching bindings against each other is a public concept
        internal bool Matches(ref InputBinding other)
        {
            if (path != null)
            {
                ////TODO: handle things like ignoring leading '/'
                if (other.path == null
                    || !StringHelpers.CharacterSeparatedListsHaveAtLeastOneCommonElement(path, other.path, Separator))
                    return false;
            }

            if (action != null)
            {
                ////TODO: handle "map/action" format
                ////TODO: handle "map/*" format
                ////REVIEW: this will not be able to handle cases where one binding references an action by ID and the other by name but both do mean the same action
                if (other.action == null
                    || !StringHelpers.CharacterSeparatedListsHaveAtLeastOneCommonElement(action, other.action, Separator))
                    return false;
            }

            if (groups != null)
            {
                if (other.groups == null
                    || !StringHelpers.CharacterSeparatedListsHaveAtLeastOneCommonElement(groups, other.groups, Separator))
                    return false;
            }

            if (!string.IsNullOrEmpty(m_Id))
            {
                if (other.id != id)
                    return false;
            }

            return true;
        }

        [Flags]
        internal enum Flags
        {
            None = 0,

            /// <summary>
            /// This and the next binding in the list combine such that both need to be
            /// triggered to trigger the associated action.
            /// </summary>
            /// <remarks>
            /// The order in which the bindings trigger does not matter.
            ///
            /// An arbitrarily long sequence of bindings can be arranged as having to trigger
            /// together.
            ///
            /// If this is set, <see cref="ThisAndPreviousCombine"/> has to be set on the
            /// subsequent binding.
            /// </remarks>
            ThisAndNextCombine = 1 << 5,
            ThisAndNextAreExclusive = 1 << 6,

            // This binding and the previous one in the list are a combo. This one
            // can only trigger after the previous one already has.
            ThisAndPreviousCombine = 1 << 0,
            ThisAndPreviousAreExclusive = 1 << 1,

            /// <summary>
            /// Whether this binding starts a composite binding group.
            /// </summary>
            /// <remarks>
            /// This flag implies <see cref="PushBindingLevel"/>. The composite is comprised
            /// of all bindings at the same grouping level. The name of each binding in the
            /// composite is used to determine which role the resolved controls play in the
            /// composite.
            /// </remarks>
            Composite = 1 << 2,
            PartOfComposite = 1 << 3,////REVIEW: remove and replace with PushBindingLevel and PopBindingLevel?

            /// <summary>
            ///
            /// </summary>
            PushBindingLevel = 1 << 3,
            PopBindingLevel = 1 << 4,
        }
    }
}
